namespace MiniHttpServer.ORM;

public interface IRecordReader
{
    int FieldCount { get; }

    bool Read();

    string GetName(int index);

    bool IsDBNull(int index);

    object GetValue(int index);
}
