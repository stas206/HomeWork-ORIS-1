using MiniHttpServer.ORM;

namespace MiniHttpServer.ORM.UnitTests;

public sealed class FakeRecordReader : IRecordReader
{
    private readonly string[] _columns;
    private readonly object?[][] _rows;
    private int _current = -1;

    public FakeRecordReader(string[] columns, params object?[][] rows)
    {
        _columns = columns;
        _rows = rows;
    }

    public int FieldCount => _columns.Length;

    public bool Read()
    {
        _current++;
        return _current < _rows.Length;
    }

    public string GetName(int index) => _columns[index];

    public bool IsDBNull(int index) => _rows[_current][index] is null or DBNull;

    public object GetValue(int index) => _rows[_current][index] ?? DBNull.Value;

    public static FakeRecordReader Single(string[] columns, object?[] row)
        => new(columns, new[] { row });

    public static FakeRecordReader Empty(string[] columns)
        => new(columns);
}
