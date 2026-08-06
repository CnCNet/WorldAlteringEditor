namespace MapEditorLibrary.Mutations;

public interface ICheckableMutation : IMutation
{
    bool ShouldPerform();
}
