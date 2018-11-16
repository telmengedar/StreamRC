using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Core.Logs;
using NightlyCode.Modules;
using StreamRC.Core;
using StreamRC.Streaming.Stream;
using StreamRC.Streaming.Stream.Chat;
using StreamRC.Streaming.Videos;

namespace StreamRC.Streaming.Shouts {

    [Module(Key ="shout", AutoCreate = true)]
    public class ShoutModule {
        readonly DatabaseModule database;
        readonly VideoServiceModule videoservice;
        readonly Dictionary<string, DateTime> lasttriggers = new Dictionary<string, DateTime>();
        readonly List<Shout> shouts = new List<Shout>();
        readonly object shoutlock = new object();

        /// <summary>
        /// creates a new <see cref="ShoutModule"/>
        /// </summary>
        /// <param name="database">access to database</param>
        /// <param name="stream">access to stream</param>
        /// <param name="videoservice">access to video service</param>
        public ShoutModule(DatabaseModule database, StreamModule stream, VideoServiceModule videoservice) {
            this.database = database;
            this.videoservice = videoservice;
            database.Database.UpdateSchema<Shout>();
            shouts.AddRange(database.Database.LoadEntities<Shout>().Execute());
            stream.ChatMessage += OnChatMessage;
            stream.RegisterCommandHandler("shouts", new ListShoutsHandler(this));
            stream.RegisterCommandHandler("shoutinfo", new ShoutInfoHandler(this));
        }

        /// <summary>
        /// shouts which can get triggered
        /// </summary>
        public IEnumerable<Shout> Shouts
        {
            get
            {
                lock(shoutlock)
                    foreach(Shout shout in shouts)
                        yield return shout;
            }
        }

        /// <summary>
        /// adds a shout to be scanned for
        /// </summary>
        /// <param name="term">term which triggers the shout</param>
        /// <param name="id">id of video to play</param>
        public void AddShout(string term, string id) {
            AddShout(term, id, TimeSpan.Zero, 0, 0, 0);
        }

        /// <summary>
        /// adds a shout to be scanned for
        /// </summary>
        /// <param name="term">term which triggers the shout</param>
        /// <param name="id">id of video to play</param>
        /// <param name="starttime">time in seconds when to start playing the video</param>
        /// <param name="endtime">time in seconds when to stop playing the video</param>
        /// <param name="volume">volume of video</param>
        public void AddShout(string term, string id, double starttime, double endtime, int volume)
        {
            AddShout(term, id, TimeSpan.Zero, starttime, endtime, volume);
        }

        /// <summary>
        /// adds a shout to be scanned for
        /// </summary>
        /// <param name="term">term which triggers the shout</param>
        /// <param name="id">id of video to play</param>
        /// <param name="cooldown">cooldown which has to pass until the video can get triggered again</param>
        /// <param name="starttime">time in seconds when to start playing the video</param>
        /// <param name="endtime">time in seconds when to stop playing the video</param>
        /// <param name="volume">volume of video</param>
        public void AddShout(string term, string id, TimeSpan cooldown, double starttime, double endtime, int volume) {
            if(volume < 0 || volume > 100)
                throw new ArgumentException("volume");

            term = term.ToLower();
            if(database.Database.Update<Shout>().Set(s => s.VideoId == id, s=>s.Cooldown==cooldown, s=>s.StartSeconds==starttime, s=>s.EndSeconds==endtime, s=>s.Volume==volume).Where(s => s.Term == term).Execute() == 0) {
                database.Database.Insert<Shout>().Columns(s => s.Term, s => s.VideoId, s=>s.Cooldown, s=>s.StartSeconds, s=>s.EndSeconds, s=>s.Volume).Values(term, id,cooldown, starttime, endtime, volume).Execute();
                lock(shoutlock)
                    shouts.Add(new Shout {
                        Term = term,
                        VideoId = id,
                        Cooldown = cooldown,
                        StartSeconds = starttime,
                        EndSeconds = endtime,
                        Volume = volume
                    });
            }

            lock(shoutlock) {
                Shout shout = shouts.FirstOrDefault(s => s.Term == term);
                if(shout != null) {
                    shout.VideoId = id;
                    shout.Cooldown = cooldown;
                    shout.StartSeconds = starttime;
                    shout.EndSeconds = endtime;
                    shout.Volume = volume;
                }
            }
        }

        /// <summary>
        /// removes a shout from service
        /// </summary>
        /// <param name="term">shout term to remove</param>
        public void RemoveShout(string term) {
            term = term.ToLower();
            database.Database.Delete<Shout>().Where(s => s.Term == term).Execute();
            lock(shoutlock) {
                shouts.RemoveAll(s => s.Term == term);
            }
        }

        void OnChatMessage(IChatChannel channel, ChatMessage message)
        {
            lock(shoutlock) {
                string messagedata = message.Message.ToLower();
                Shout shout = shouts.FirstOrDefault(s => messagedata.StartsWith(s.Term));
                if(shout == null)
                    return;

                if(shout.Cooldown.Ticks > 0) {
                    lasttriggers.TryGetValue(shout.Term, out DateTime lasttrigger);
                    if(DateTime.Now - lasttrigger < shout.Cooldown) {
                        TimeSpan cooldown = shout.Cooldown - (DateTime.Now - lasttrigger);
                        Logger.Warning(this, $"{message.Service}:{message.User} tried to shout '{shout.Term}' but needs to wait another {cooldown.TotalMinutes} minutes to do that.");
                        channel.SendMessage($"You need to wait another {cooldown.TotalMinutes} minutes before shouting '{shout.Term}' again.");
                        return;
                    }

                    lasttriggers[shout.Term] = DateTime.Now;
                }

                videoservice.AddVideo(shout.VideoId, shout.StartSeconds, shout.EndSeconds, shout.Volume);
            }
        }

    }
}