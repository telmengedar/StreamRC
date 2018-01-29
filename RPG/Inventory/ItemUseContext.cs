using System.Collections;
using System.Text;
using StreamRC.RPG.Items;
using StreamRC.RPG.Messages;
using StreamRC.Streaming.Users;

namespace StreamRC.RPG.Inventory {
    public class ItemUseContext {
        readonly RPGMessageBuilder message;
        User user;
        Item item;

        public ItemUseContext(RPGMessageBuilder message, User user, Item item) {
            this.message = message;
            this.user = user;
            this.item = item;
        }

        public void Send(string text) {
            StringBuilder builder=new StringBuilder();
            StringBuilder fieldbuilder=new StringBuilder();
            bool field = false;

            foreach(char character in text)
            {
                switch(character) {
                    case '{':
                        if(field && fieldbuilder.Length == 0) {
                            builder.Append('{');
                            field = false;
                            continue;
                        }

                        field = true;
                        break;
                    case '}':
                        if(field) {
                            if (builder.Length > 0)
                            {
                                message.Text(builder.ToString());
                                builder.Length = 0;
                            }

                            if(fieldbuilder.Length > 0) {
                                switch(fieldbuilder.ToString().ToLower()) {
                                    case "user":
                                        message.User(user);
                                        break;
                                    case "item":
                                        message.Item(item);
                                        break;
                                }
                                fieldbuilder.Length = 0;
                            }
                            field = false;
                        }
                        else {
                            builder.Append('}');
                        }
                        break;
                    default:
                        if(field)
                            fieldbuilder.Append(character);
                        else builder.Append(character);
                        break;
                }
            }

            if(builder.Length > 0)
                message.Text(builder.ToString());

            message.Send();
        }
    }
}