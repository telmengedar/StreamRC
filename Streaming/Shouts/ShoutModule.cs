using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using NightlyCode.Modules.Dependencies;
using NightlyCode.StreamRC.Modules;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Videos;

namespace StreamRC.Streaming.Shouts {

    [Dependency(nameof(StreamModule))]
    [Dependency(nameof(VideoServiceModule))]
    [ModuleKey("shout")]
    public class ShoutModule : IInitializableModule, IRunnableModule {
        readonly Context context;

        readonly Dictionary<string, DateTime> lasttriggers = new Dictionary<string, DateTime>();
        readonly List<Shout> shouts = new List<Shout>();
        readonly object shoutlock = new object();

        /// <summary>
        /// creates a new <see cref="ShoutModule"/>
        /// </summary>
        /// <param name="context">access to modules</param>
        public ShoutModule(Context context) {
            this.context = context;
        }

        /// <summary>
        /// adds a shout to be scanned for
        /// </summary>
        /// <param name="term">term which triggers the shout</param>
        /// <param name="id">id of video to play</param>
        public void AddShout(string term, string id) {
            AddShout(term, id, TimeSpan.Zero, 0, 0);
        }

        /// <summary>
        /// adds a shout to be scanned for
        /// </summary>
        /// <param name="term">term which triggers the shout</param>
        /// <param name="id">id of video to play</param>
        /// <param name="starttime">time in seconds when to start playing the video</param>
        /// <param name="endtime">time in seconds when to stop playing the video</param>
        public void AddShout(string term, string id, double starttime, double endtime)
        {
            AddShout(term, id, TimeSpan.Zero, starttime, endtime);
        }

        /// <summary>
        /// adds a shout to be scanned for
        /// </summary>
        /// <param name="term">term which triggers the shout</param>
        /// <param name="id">id of video to play</param>
        /// <param name="cooldown">cooldown which has to pass until the video can get triggered again</param>
        /// <param name="starttime">time in seconds when to start playing the video</param>
        /// <param name="endtime">time in seconds when to stop playing the video</param>
        public void AddShout(string term, string id, TimeSpan cooldown, double starttime, double endtime) {
            term = term.ToLower();
            if(context.Database.Update<Shout>().Set(s => s.VideoId == id, s=>s.Cooldown==cooldown, s=>s.StartSeconds==starttime, s=>s.EndSeconds==endtime).Where(s => s.Term == term).Execute() == 0) {
                context.Database.Insert<Shout>().Columns(s => s.Term, s => s.VideoId, s=>s.Cooldown, s=>s.StartSeconds, s=>s.EndSeconds).Values(term, id,cooldown, starttime, endtime).Execute();
                lock(shoutlock)
                    shouts.Add(new Shout {
                        Term = term,
                        VideoId = id,
                        Cooldown = cooldown,
                        StartSeconds = starttime,
                        EndSeconds = endtime
                    });
            }

            lock(shoutlock) {
                Shout shout = shouts.FirstOrDefault(s => s.Term == term);
                if(shout != null) {
                    shout.VideoId = id;
                    shout.Cooldown = cooldown;
                    shout.StartSeconds = starttime;
                    shout.EndSeconds = endtime;
                }
            }
        }

        /// <summary>
        /// removes a shout from service
        /// </summary>
        /// <param name="term">shout term to remove</param>
        public void RemoveShout(string term) {
            term = term.ToLower();
            context.Database.Delete<Shout>().Where(s => s.Term == term).Execute();
            lock(shoutlock) {
                shouts.RemoveAll(s => s.Term == term);
            }
        }

        void IInitializableModule.Initialize() {
            context.Database.UpdateSchema<Shout>();
        }

        void IRunnableModule.Start() {
            lock(shoutlock) {
                shouts.Clear();
                shouts.AddRange(context.Database.LoadEntities<Shout>().Execute());
            }

            context.GetModule<StreamModule>().ChatMessage += OnChatMessage;
        }

        void IRunnableModule.Stop() {
            context.GetModule<StreamModule>().ChatMessage -= OnChatMessage;
        }

        void OnChatMessage(ChatMessage message)
        {
            lock(shoutlock) {
                string messagedata = message.Message.ToLower();
                Shout shout = shouts.FirstOrDefault(s => messagedata.StartsWith(s.Term));
                if(shout == null)
                    return;

                if(shout.Cooldown.Ticks > 0) {
                    DateTime lasttrigger;
                    lasttriggers.TryGetValue(shout.Term, out lasttrigger);
                    if(DateTime.Now - lasttrigger < shout.Cooldown) {
                        TimeSpan cooldown = shout.Cooldown - (DateTime.Now - lasttrigger);
                        Logger.Warning(this, $"{message.Service}:{message.User} tried to shout '{shout.Term}' but needs to wait another {cooldown.TotalMinutes} minutes to do that.");
                        context.GetModule<StreamModule>().SendMessage(message.Service, message.Channel, message.User, $"You need to wait another {cooldown.TotalMinutes} minutes before shouting '{shout.Term}' again.");
                        return;
                    }

                    lasttriggers[shout.Term] = DateTime.Now;
                }

                context.GetModule<VideoServiceModule>().AddVideo(shout.VideoId, shout.StartSeconds, shout.EndSeconds);
            }
        }

    }
}