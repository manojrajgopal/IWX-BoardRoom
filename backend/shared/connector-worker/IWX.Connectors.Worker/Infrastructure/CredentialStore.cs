using MongoDB.Driver;

namespace IWX.Connectors.Worker.Infrastructure;

public sealed class CredentialStore
{
    private readonly IMongoCollection<ConnectorCredential> _col;

    public CredentialStore(IMongoDatabase db)
    {
        _col = db.GetCollection<ConnectorCredential>("credentials");
    }

    public async Task<ConnectorCredential?> GetAsync(string account, CancellationToken ct = default) =>
        await _col.Find(c => c.Account == account).FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(ConnectorCredential cred, CancellationToken ct = default)
    {
        cred.UpdatedAtUtc = DateTime.UtcNow;
        await _col.ReplaceOneAsync(
            c => c.Account == cred.Account,
            cred,
            new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task<IReadOnlyList<string>> ListAccountsAsync(CancellationToken ct = default)
    {
        var docs = await _col.Find(FilterDefinition<ConnectorCredential>.Empty)
            .Project(c => c.Account).ToListAsync(ct);
        return docs;
    }

    public async Task<bool> DeleteAsync(string account, CancellationToken ct = default)
    {
        var r = await _col.DeleteOneAsync(c => c.Account == account, ct);
        return r.DeletedCount > 0;
    }
}
