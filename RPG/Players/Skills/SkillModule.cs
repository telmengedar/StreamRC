using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Database.Entities.Operations.Fields;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Core.Scripts;
using StreamRC.RPG.Adventure;
using StreamRC.RPG.Inventory;
using StreamRC.RPG.Messages;
using StreamRC.RPG.Players.Skills.Monster;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Players.Skills {

    [Module(Key="skills")]
    public class SkillModule : ICommandModule, IItemCommandModule {
        readonly IDatabaseModule database;
        readonly IStreamModule stream;
        readonly PlayerModule players;
        readonly RPGMessageModule messages;
        readonly AdventureModule adventure;
        readonly UserModule usermodule;

        public SkillModule(IDatabaseModule database, IStreamModule stream, PlayerModule players, RPGMessageModule messages, AdventureModule adventure, UserModule usermodule) {
            this.database = database;
            this.stream = stream;
            this.players = players;
            this.messages = messages;
            this.adventure = adventure;
            this.usermodule = usermodule;
            database.Database.UpdateSchema<PlayerSkill>();
            database.Database.UpdateSchema<SkillConsumption>();
        }

        /// <summary>
        /// triggered when the skill knowledge of a player was changed
        /// </summary>
        public event Action<long, SkillType, int> SkillChanged;

        public bool CanCastHeal(long playerid) {
            SkillConsumption skill = GetSkill(playerid, SkillType.Heal);
            if(skill == null)
                return false;

            return GetSkillCost(SkillType.Heal, skill.Level) <= players.GetPlayer(playerid).CurrentMP;
        }

        public MonsterSkill GetMonsterSkill(string name, int level) {
            switch(name) {
                case "pestilence":
                    return new PestilenceSkill(level, adventure, messages);
                case "suck":
                    return new SuckSkill(level, messages);
                case "poison":
                    return new PoisonSkill(level, adventure, messages);
                case "steal":
                    return new StealSkill(level, players, messages);
                default:
                    return null;
            }
        }

        public int GetSkillCost(SkillType skill, int level) {
            switch(skill) {
                case SkillType.Heal:
                    switch(level) {
                        case 1:
                            return 5;
                        case 2:
                            return 8;
                        case 3:
                            return 10;
                        default:
                            return 0;
                    }
                default:
                    return 0;
            }
        }

        void CastHeal(string channel, User user, Player player) {
            SkillConsumption skill = GetSkill(player.UserID, SkillType.Heal);
            if(skill == null) {
                stream.SendMessage(user.Service, channel, user.Name, $"You don't know anything about {SkillType.Heal}.");
                return;
            }

            int cost = GetSkillCost(SkillType.Heal, skill.Level);
            if(cost > player.CurrentMP) {
                stream.SendMessage(user.Service, channel, user.Name, "Not enough mana.");
                return;
            }

            int healed = 0;
            switch(skill.Level) {
                case 1:
                    healed = 5 + (int)(player.Intelligence * 0.5);
                    break;
                case 2:
                    healed = 12 + (int)(player.Intelligence * 0.83);
                    break;
                case 3:
                    healed = 30 + (int)(player.Intelligence * 1.22);
                    break;
            }

            healed = Math.Min(player.MaximumHP - player.CurrentHP, healed);
            database.Database.Update<Player>().Set(p => p.CurrentHP == p.CurrentHP + healed, p => p.CurrentMP == p.CurrentMP - cost).Where(p => p.UserID == player.UserID).Execute();
            messages.Create().User(user).Text(" casts ").Skill(SkillType.Heal).Text(" and heals ").Health(healed).Text(".").Send();
        }

        public void Cast(string channel, User user, Player player, SkillType skill) {
            switch(skill) {
                default:
                    throw new Exception($"{skill} is an unknown skill and can't be cast.");
                case SkillType.LuckyBastard:
                case SkillType.Mule:
                    throw new Exception($"{skill} is a passive skill and can't be cast.");
                case SkillType.Heal:
                    CastHeal(channel, user, player);
                    break;
            }
        }

        public int GetUsedSkillPoints(long playerid) {
            return database.Database.Load<SkillConsumption>(s=>DBFunction.Sum(s.Consumption)).Where(s => s.PlayerID == playerid).ExecuteScalar<int>();
        }

        public int GetSkillLevel(long playerid, SkillType skill) {
            return database.Database.Load<PlayerSkill>(s => s.Level).Where(s => s.PlayerID == playerid && s.Skill == skill).ExecuteScalar<int>();
        }

        public SkillConsumption GetSkill(long playerid, SkillType skill) {
            return database.Database.LoadEntities<SkillConsumption>().Where(s => s.PlayerID == playerid && s.Skill == skill).Execute().FirstOrDefault();
        }

        public IEnumerable<SkillConsumption> GetSkills(long playerid) {
            return database.Database.LoadEntities<SkillConsumption>().Where(s => s.PlayerID == playerid).Execute();
        }

        void ForgetSkill(string service, string username, string skill)
        {
            Player player = players.GetExistingPlayer(service, username);
            SkillType skilltype = (SkillType)Enum.Parse(typeof(SkillType), skill, true);

            int level = GetSkillLevel(player.UserID, skilltype);
            if(level <= 0)
                throw new StreamCommandException($"You don't know anything of {skilltype}");

            database.Database.Update<PlayerSkill>().Set(s => s.Level == s.Level - 1).Where(s => s.PlayerID == player.UserID && s.Skill == skilltype).Execute();
            database.Database.Delete<PlayerSkill>().Where(s => s.Level <= 0).Execute();

            User user = usermodule.GetExistingUser(service, username);
            messages.Create().User(user).Text(" forgets something about ").Skill(skilltype).Text(".").Send();
            SkillChanged?.Invoke(player.UserID, skilltype, level - 1);
        }

        void LearnSkill(string service, string username, string skill)
        {
            Player player = players.GetExistingPlayer(service, username);
            User user = usermodule.GetUser(player.UserID);
            SkillType skilltype = (SkillType)Enum.Parse(typeof(SkillType), skill, true);

            int skillevel = GetSkillLevel(player.UserID, skilltype);
            if (skillevel >= 3)
                throw new StreamCommandException($"You know everything there is to know about {skilltype}.");

            int skillconsumption = GetUsedSkillPoints(player.UserID);
            int skillpointsleft = player.Level - skillconsumption;
            int nextlevel = skillevel + 1;
            if(skillpointsleft < GetSkillpointRequirement(nextlevel)) {
                messages.Create().User(user).Text(" doesn't have enough skillpoints to learn ").Skill(skilltype).Send();
                return;
            }

            if(database.Database.Update<PlayerSkill>().Set(s => s.Level == s.Level + 1).Where(s => s.PlayerID == player.UserID && s.Skill == skilltype).Execute() == 0)
                database.Database.Insert<PlayerSkill>().Columns(s => s.PlayerID, s => s.Skill, s => s.Level).Values(player.UserID, skilltype, nextlevel).Execute();

            messages.Create().User(user).Text(" learned ").Skill(skilltype).Text($" level {nextlevel}.").Send();

            SkillChanged?.Invoke(player.UserID, skilltype, nextlevel);
        }

        public int GetSkillpointRequirement(int level) {
            switch(level) {
                case 1:
                case 2:
                case 3:
                    return level;                
                default:
                    throw new Exception($"It is not possible to learn a skill level {level}.");
            }
        }

        public void ModifyPlayerStats(Player player) {
            foreach(SkillConsumption skill in GetSkills(player.UserID))
                switch(skill.Skill) {
                    case SkillType.LuckyBastard:
                        switch(skill.Level) {
                            case 1:
                                player.Luck += 5;
                                break;
                            case 2:
                                player.Luck += 13;
                                break;
                            case 3:
                                player.Luck += 35;
                                break;
                        }
                        break;
                }
        }

        void ICommandModule.ProcessCommand(string command, params string[] arguments) {
            switch(command) {
                case "forget":
                    if(arguments.Length < 3)
                        throw new StreamCommandException("You have to specify the skill name to forget.");

                    ForgetSkill(arguments[0], arguments[1], arguments[2]);
                    break;
                case "learn":
                    if (arguments.Length < 3)
                        throw new StreamCommandException("You have to specify the skill name to learn.");

                    LearnSkill(arguments[0], arguments[1], arguments[2]);
                    break;
            }
        }

        [Command("cast", "$service", "$channel", "$user")]
        public void CastSpell(string service, string channel, string username, string[] arguments) {
            if(arguments.Length < 1) {
                stream.SendMessage(service, channel, username, "You have to specify the name of the spell to cast -> !cast <SPELL>");
                return;
            }

            SkillType skill;
            try {
                skill = (SkillType)Enum.Parse(typeof(SkillType), arguments[0], true);
            }
            catch(Exception) {
                stream.SendMessage(service, channel, username, $"'{arguments[0]}' is not a spell (at least not a spell known around here).");
                return;
            }

            User user = usermodule.GetExistingUser(service, username);
            Player player = players.GetExistingPlayer(user.ID);
            if(player == null) {
                stream.SendMessage(service, channel, username, "You are not a player in this rpg.");
                return;
            }

            Cast(channel, user, player, skill);
        }

        [Command("skills", "$service", "$channel", "$user")]
        public void ShowSkillList(string service, string channel, string user) {
            Player player = players.GetExistingPlayer(service, user);
            SkillConsumption[] skills = GetSkills(player.UserID).ToArray();
            string skillist = string.Join(", ", skills.Select(s => $"{s.Skill} {s.Level}"));
            int left = player.Level - skills.Sum(s => s.Consumption);
            stream.SendMessage(service, channel, user, $"{skillist}. Skillpoints Left: {left}.");
        }

        void IItemCommandModule.ExecuteItemCommand(User user, Player player, string command, params string[] arguments) {
            switch(command) {
                case "forget":
                    ForgetSkill(user.Service, user.Name, arguments[0]);
                    break;
                case "learn":
                    LearnSkill(user.Service, user.Name, arguments[0]);
                    break;
            }
        }
    }
}