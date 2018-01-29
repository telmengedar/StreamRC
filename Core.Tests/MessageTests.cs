using System.Collections.Generic;
using NUnit.Framework;
using StreamRC.Core.Messages;

namespace Core.Tests
{

    [TestFixture]
    public class MessageTests
    {

        IEnumerable<string> Messages
        {
            get
            {
                yield return "[b]Rugenforth[/] has found [c=FFFFB0]73 Gold[/].";
                yield return "[b]Rugenforth[/] has found [c=D0D0FF]1 Wood[/] and is now encumbered.";
                yield return "[b]Rugenforth[/] is encumbered and dropped [c=D0D0FF]1 Paper[/].";
                yield return "[b]Rugenforth[/] has found [c=D0D0FF]1 Metal[/].";
            }    
        }

        [Test]
        public void ParseMessages([ValueSource(nameof(Messages))] string data) {
            Message message = MessageExtensions.Parse(data);
        }
    }
}
