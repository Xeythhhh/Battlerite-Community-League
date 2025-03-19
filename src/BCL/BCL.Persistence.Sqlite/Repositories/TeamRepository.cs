using System.Globalization;
using System.Linq.Expressions;

using BCL.Domain.Entities.Users;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BCL.Persistence.Sqlite.Repositories;
public class TeamRepository(BclDbContext dbContext) : GenericRepository<PremadeTeam>(dbContext), ITeamRepository
{
    public override PremadeTeam? GetById(Ulid id)
    {
        PremadeTeam? team = DbSet.Find(id);
        if (team == null) return team;

        team.Members = team.MemberIds.Split('|')
            .Select(ulong.Parse)
            .Select(userId => DbContext.Users.Single(u => u.DiscordId == userId))
            .ToList();

        return team;
    }

    public override PremadeTeam? GetById(string id) => !Ulid.TryParse(id, out Ulid ulid) ? null : GetById(ulid);

    public override IEnumerable<PremadeTeam> Get(Expression<Func<PremadeTeam, bool>> predicate)
    {
        List<PremadeTeam> teams = base.Get(predicate).ToList();
        foreach (PremadeTeam? team in teams)
        {
            team.Members = team.MemberIds.Split('|')
                        .Select(ulong.Parse)
                        .Select(userId => DbContext.Users.Single(u => u.DiscordId == userId))
                        .ToList();
        }
        return teams;
    }

    #region not implemented
    public override ValueTask<PremadeTeam?> GetByIdAsync(string id) => throw new NotImplementedException();
    public override ValueTask<PremadeTeam?> GetByIdAsync(Ulid id) => throw new NotImplementedException();
    #endregion

    public override EntityEntry<PremadeTeam> Add(PremadeTeam entity)
    {
        ParseMembers(entity);
        return base.Add(entity);
    }

    public override ValueTask<EntityEntry<PremadeTeam>> AddAsync(PremadeTeam entity)
    {
        ParseMembers(entity);
        return base.AddAsync(entity);
    }

    public override void AddRange(IEnumerable<PremadeTeam> entities)
    {
        PremadeTeam[] premadeTeams = entities as PremadeTeam[] ?? entities.ToArray();
        foreach (PremadeTeam entity in premadeTeams) ParseMembers(entity);

        base.AddRange(premadeTeams);
    }

    public override Task AddRangeAsync(IEnumerable<PremadeTeam> entities)
    {
        PremadeTeam[] premadeTeams = entities as PremadeTeam[] ?? entities.ToArray();
        foreach (PremadeTeam entity in premadeTeams) ParseMembers(entity);

        return base.AddRangeAsync(premadeTeams);
    }

    private static void ParseMembers(PremadeTeam entity)
    {
        entity.MemberIds = entity.Members.Select(m => m.DiscordId).Aggregate("",
            (current, next) => $"{current}|{next}").Trim('|');
    }

    public IEnumerable<PremadeTeam> GetByDiscordUser(DiscordUser discordUser) => GetByDiscordUserId(discordUser.Id);
    public IEnumerable<PremadeTeam> GetByDiscordUserId(ulong discordUserId)
    {
        List<PremadeTeam> teams = base.Get(t => t.MemberIds.Contains(discordUserId.ToString(CultureInfo.InvariantCulture))).ToList();
        foreach (PremadeTeam? team in teams)
        {
            team.Members = team.MemberIds.Split('|')
                .Select(ulong.Parse)
                .Select(userId => DbContext.Users.Single(u => u.DiscordId == userId))
                .ToList();
        }
        return teams;
    }
}
