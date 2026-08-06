namespace MapEditorLibrary.Mutations;

public interface IMutation
{
    int EventID { get; }
    MutationHistoryMetadata HistoryMetadata { get; }

    string GetDisplayString();
    void Perform();
    void Undo();
}
