using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.Attributes;

public abstract class BaseAttribute
{
    public static Task Respond(BaseContext context, string content)
    {
        //it just works
        try
        {
            return Task.WhenAll(context.CreateResponseAsync(content));
        }
        catch (Exception)
        {
            return Task.WhenAll(context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(content)));
        }
    }
}
