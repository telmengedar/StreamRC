using System;
using System.Collections.Generic;
using System.Linq;

namespace StreamRC.Streaming.Polls {

    /// <summary>
    /// collects votes and computes a result for all votes
    /// </summary>
    public class PollDiagramData : IDiagramData {
        readonly object votelock = new object();
        readonly Dictionary<string, WeightedVote> votes = new Dictionary<string, WeightedVote>();
        PollOption[] options;

        /// <summary>
        /// creates new <see cref="PollDiagramData"/> based on existing votes
        /// </summary>
        /// <param name="votes">votes to be added</param>
        public PollDiagramData(IEnumerable<WeightedVote> votes)
        {
            foreach(WeightedVote vote in votes)
                SetVote(vote);
        }

        /// <summary>
        /// clears the poll data
        /// </summary>
        public void Clear() {
            lock(votelock)
                votes.Clear();
        }

        /// <summary>
        /// sets a vote for a user
        /// </summary>
        /// <param name="vote">vote to update poll with</param>
        public void SetVote(WeightedVote vote) {
            lock(votelock)
                votes[vote.User] = vote;
        }

        /// <summary>
        /// computes a result for the poll
        /// </summary>
        /// <param name="count">maximum number of results to return (optional, defaults to 5)</param>
        /// <returns></returns>
        public IEnumerable<DiagramItem> GetItems(int count=5) {
            Dictionary<string, int> votecount=new Dictionary<string, int>();
            lock(votelock) {
                foreach(IGrouping<string, KeyValuePair<string, WeightedVote>> valuegroup in votes.GroupBy(v => v.Value.Vote))
                    votecount[valuegroup.Key] = valuegroup.Sum(v => v.Value.Status);
                if(options != null) {
                    foreach(PollOption option in options)
                        if(!votecount.ContainsKey(option.Key))
                            votecount[option.Key] = 0;
                }
            }

            int max = -1;
            foreach(KeyValuePair<string, int> result in votecount.OrderByDescending(v => v.Value).Take(count)) {
                if(max == -1)
                    max = result.Value;

                yield return new DiagramItem {
                    Item = result.Key,
                    Count = result.Value,
                    Percentage = (float)result.Value / Math.Max(max, 1)
                };
            }
        }

        /// <summary>
        /// adds additional options to the diagram
        /// </summary>
        /// <param name="options">options to be included in the diagram, regardless of the votecount</param>
        public void AddOptions(PollOption[] options) {
            this.options = options;
        }
    }
}