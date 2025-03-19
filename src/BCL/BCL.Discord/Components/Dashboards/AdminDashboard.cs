using DSharpPlus.Entities;

namespace BCL.Discord.Components.Dashboards;

public class AdminDashboard
{
    //todo lmao
    // ReSharper disable once NotAccessedField.Local
#pragma warning disable CS8618
    private IServiceProvider _services;
#pragma warning restore CS8618
#pragma warning disable CS8618
    public DiscordChannel Channel { get; set; }
#pragma warning restore CS8618
    public DiscordMessage? DiscordMessage { get; set; }

    public async Task Setup(IServiceProvider services, DiscordChannel adminChannel)
    {
        _services = services;
        Channel = adminChannel;

        IReadOnlyList<DiscordMessage>? messagesToDelete = await Channel.GetMessagesAsync();
        if (messagesToDelete is not null && messagesToDelete.Count != 0)
            await Channel.DeleteMessagesAsync(messagesToDelete);

        await Update();
    }

    public async Task Update()
    {
        DiscordMessageBuilder message = new DiscordMessageBuilder()
                .WithEmbed(GetEmbed())
            //.AddComponents(QueueButtons)
            //.AddComponents(QuickActionsButtons);
            ;

        if (DiscordMessage is null)
        {
            DiscordMessage = await message.SendAsync(Channel);
        }
        else
        {
            await DiscordMessage.ModifyAsync(message);
        }
    }

    private DiscordEmbedBuilder GetEmbed()
    {
        DiscordEmbedBuilder embed = new();

        return embed;
    }
}
