using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using BCL.Common.Extensions;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;

using Microsoft.Extensions.DependencyInjection;

namespace BCL.Discord.Components;
public class ProLeagueManager
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IServiceProvider _services;
    public DiscordChannel GeneralChannel { get; set; }
    public DiscordChannel EuChannel { get; set; }
    public DiscordChannel NaChannel { get; set; }
    public DiscordChannel SaChannel { get; set; }
    public DiscordChannel EuAdminChannel { get; set; }
    public DiscordChannel NaAdminChannel { get; set; }
    public DiscordChannel SaAdminChannel { get; set; }
    public Dictionary<ulong, Application> Applications { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public void Setup(
        IServiceProvider services,
        DiscordChannel generalChannel,
        DiscordChannel euChannel,
        DiscordChannel naChannel,
        DiscordChannel saChannel,
        DiscordChannel euAdminChannel,
        DiscordChannel naAdminChannel,
        DiscordChannel saAdminChannel)
    {
        _services = services;
        GeneralChannel = generalChannel;
        EuChannel = euChannel;
        NaChannel = naChannel;
        SaChannel = saChannel;
        EuAdminChannel = euAdminChannel;
        NaAdminChannel = naAdminChannel;
        SaAdminChannel = saAdminChannel;
        Applications = [];
    }

    public class Application(DiscordMessage message, Region region)
    {
        public ulong DiscordId { get; set; }
        public DiscordMessage? Message { get; set; } = message;
        public DiscordMessage? AdminMessage { get; set; }
        public List<Vote> Votes { get; set; } = [];
        public Region Region { get; set; } = region;

        public record Vote(ulong DiscordId, bool Approve);
    }

    public async Task<Application> AddApplication(DiscordMessage message, ulong discordId, Region region, bool restart = false)
    {
        Applications.Add(discordId, new Application(message, region));
        Application application = Applications[discordId];

        if (restart)
        {
            await using AsyncServiceScope scope = _services.CreateAsyncScope();
            IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            Domain.Entities.Users.User user = userRepository.GetByDiscordId(discordId) ??
                       throw new InvalidOperationException("User is not registered");

            if (!string.IsNullOrEmpty(user.ProApplications))
            {
                DiscordChannel channel = region switch
                {
                    Region.Eu => EuAdminChannel,
                    Region.Na => NaAdminChannel,
                    Region.Sa => SaAdminChannel,

                    Region.Unknown => throw new NotImplementedException(),
                    _ => throw new ArgumentOutOfRangeException(nameof(region), region, null)
                };

                DiscordMessage previousAdminMessage = await channel.GetMessageAsync(
                    user.ProApplications.Split("|")
                        .Select(ulong.Parse).Last());

                try
                {
                    string decrypted = Decrypt(previousAdminMessage.Embeds[0].Fields[0].Value);
                    string[] votes = decrypted.Split('|');
                    foreach (string vote in votes)
                    {
                        string[] values = vote.Split("\\");
                        application.Votes.Add(new Application.Vote(ulong.Parse(values[0]), bool.Parse(values[1])));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                throw new Exception("Previous info was not saved");
            }
        }

        application.AdminMessage = await UpdateAdminMessage(discordId, message, region, true);

        return application;
    }

    public async Task<DiscordMessage> UpdateAdminMessage(ulong discordId)
    {
        Application application = Applications[discordId];
        return await UpdateAdminMessage(discordId, application.Message!, application.Region);
    }

    private const string HashKey = "supersecretkey123";
    private async Task<DiscordMessage> UpdateAdminMessage(ulong discordId, DiscordMessage message, Region region, bool newApplication = false)
    {
        Applications.TryGetValue(discordId, out Application? application);
        DiscordEmbedBuilder embed = new(application?.AdminMessage?.Embeds[0] ?? message.Embeds[0]);

        embed.Fields[0].Name = "Hash";
        embed.Fields[0].Value = ParseVotes();

        if (!newApplication)
        {
            embed.Fields[1].Value = GetInsight(application);

            application!.AdminMessage = await application.AdminMessage!.ModifyAsync(new DiscordMessageBuilder()
                .AddEmbed(embed));

            return application.AdminMessage;
        }

        embed.AddField("Insight", GetInsight(application));

        DiscordChannel channel = region switch
        {
            Region.Eu => EuAdminChannel,
            Region.Na => NaAdminChannel,
            Region.Sa => SaAdminChannel,

            Region.Unknown => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(region), region, null)
        };

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        Domain.Entities.Users.User user = userRepository.GetByDiscordId(discordId) ?? throw new InvalidOperationException("User is not registered");

        if (!string.IsNullOrEmpty(user.ProApplications))
        {
            string existingInfo = string.Empty;
            int index = 0;
            string fixProApplicationLinks = string.Empty;

            foreach (ulong messageId in user.ProApplications.Split("|")
                         .OrderDescending()
                         .Select(ulong.Parse)
                         .ToList())
            {
                DiscordMessage? existingAdminMessage = null;

                try //if it's too old or locked you can't edit
                {
                    existingAdminMessage = await channel.GetMessageAsync(messageId);

                    await existingAdminMessage.ModifyAsync(new DiscordMessageBuilder()
                        .WithContent("OUTDATED, new application was submitted")
                        .WithEmbed(existingAdminMessage.Embeds[0]));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if (existingAdminMessage == null) continue;

                existingInfo += $"\n{existingAdminMessage.Timestamp.DateTime.DiscordTime()}{existingAdminMessage.JumpLink.DiscordLink(index++ is 0 ? "Latest" : $"Review-{index}")}";
                fixProApplicationLinks += $"|{existingAdminMessage.Id}";
            }

            if (string.IsNullOrWhiteSpace(existingInfo))
            {
                user.ProApplications = string.Empty;
            }
            else
            {
                embed.AddField("Previous reviews", existingInfo);
                user.ProApplications = fixProApplicationLinks.Trim('|');
            }
        }

        DiscordMessage adminMessage = await new DiscordMessageBuilder()
            .AddEmbed(embed)
            .SendAsync(channel);

        user.ProApplications = (user.ProApplications + $"|{adminMessage.Id}").Trim('|');
        await userRepository.SaveChangesAsync();

        return adminMessage;

        static string GetInsight(Application? app) =>
            $"""
             ```diff
             + Approve: {app?.Votes.Count(v => v.Approve) ?? 0}
             - Decline: {app?.Votes.Count(v => !v.Approve) ?? 0}
             ```
             """;

        string ParseVotes() => $"||{Encrypt(application?.Votes.Aggregate("", (c, n) => $@"{c}|{n.DiscordId}\{n.Approve}").Trim('|'))}||";
    }

    public async Task Vote(ButtonContext context, ulong applicantId, ulong userId, bool approve, Region region, IUserRepository userRepository)
    {
        if (Applications.Any(a => a.Key == applicantId && a.Value.Votes.Any(v => v.DiscordId == userId && v.Approve == approve))) return;

        if (!userRepository.GetByDiscordId(userId)?.Pro ?? false)
            throw new InvalidOperationException("Only Pros can review applications");

        DiscordEmbedBuilder embed = new(context.Message.Embeds[0]);

        ulong regionRoleId = region switch
        {
            Region.Eu => DiscordConfig.Roles.Region.EuId,
            Region.Na => DiscordConfig.Roles.Region.NaId,
            Region.Sa => DiscordConfig.Roles.Region.SaId,

            Region.Unknown => throw new NotImplementedException(),
            _ => throw new UnreachableException()
        };

        if (!Applications.TryGetValue(applicantId, out Application? application))
        {
            Domain.Entities.Users.User? applicant = userRepository.GetByDiscordId(applicantId);

            if (applicant is null || applicant.Pro) throw new InvalidOperationException($"<@{applicantId}> does not have a pending application.");

            DiscordMessage message = await context.Message.ModifyAsync(GetMessage(context.Message.Content));

            application = await AddApplication(message, applicantId, region, true);
        }

        Application.Vote? previousVote = application.Votes.FirstOrDefault(v => v.DiscordId == userId);
        if (previousVote != null) { application.Votes.Remove(previousVote); }

        application.Votes.Add(new Application.Vote(userId, approve));

        embed.Fields[0].Value =
            $"{application.Votes.Count} / {userRepository.GetAll().Count(u => u.Server == region && u.Pro)} Players";

        application.Message = await application.Message!.ModifyAsync(
            GetMessage(context.Message.Content));

        application.AdminMessage = await UpdateAdminMessage(applicantId);
        return;

        DiscordMessageBuilder GetMessage(string content) =>
            new DiscordMessageBuilder()
                .WithContent(content)
                .AddEmbed(embed)
                .AddMention(new RoleMention(regionRoleId))
                .AddComponents(context.Message.Components);
    }

    static string? Encrypt(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return plainText;

        Console.WriteLine($"Original PlainText: {plainText}");

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = GenerateKey(HashKey);
            aesAlg.IV = new byte[16];
            aesAlg.Padding = PaddingMode.PKCS7;

            Console.WriteLine($"Decryption Key: {BitConverter.ToString(aesAlg.Key)}");
            Console.WriteLine($"Decryption IV: {BitConverter.ToString(aesAlg.IV)}");

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new())
            {
                using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                }

                byte[] encryptedBytes = msEncrypt.ToArray();
                Console.WriteLine($"Encrypted Length: {encryptedBytes.Length}");
                // Optionally log a portion of the encrypted content
                Console.WriteLine($"Encrypted Content (Partial): {BitConverter.ToString(encryptedBytes.Take(16).ToArray())}...");

                return Convert.ToBase64String(encryptedBytes);
            }
        }
    }

    static string Decrypt(string cipherText)
    {
        cipherText = cipherText.Trim('|');
        Console.WriteLine($"Trimmed CipherText: {cipherText}");

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = GenerateKey(HashKey);
            aesAlg.IV = new byte[16];
            aesAlg.Padding = PaddingMode.PKCS7;

            Console.WriteLine($"Decryption Key: {BitConverter.ToString(aesAlg.Key)}");
            Console.WriteLine($"Decryption IV: {BitConverter.ToString(aesAlg.IV)}");

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            Console.WriteLine($"CipherBytes Length: {cipherBytes.Length}");

            using MemoryStream msDecrypt = new(cipherBytes);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);

            string decryptedContent = srDecrypt.ReadToEnd();
            Console.WriteLine($"Decrypted Content: {decryptedContent}");

            return decryptedContent;
        }
    }

    static byte[] GenerateKey(string password, int keySize = 256 / 8)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

        // Adjust key size if necessary
        Array.Resize(ref hashBytes, keySize);

        return hashBytes;
    }
}
