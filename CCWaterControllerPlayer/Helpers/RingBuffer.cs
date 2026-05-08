namespace CCWaterControllerPlayer.Helpers;

public class RingBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;
    private readonly object _lock = new();

    public int Capacity => _buffer.Length;
    public int Count { get { lock (_lock) return _count; } }

    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _buffer = new T[capacity];
    }

    public void Write(T item)
    {
        lock (_lock)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length)
                _count++;
        }
    }

    public T[] ReadAll()
    {
        lock (_lock)
        {
            var result = new T[_count];
            if (_count == 0) return result;

            int start = (_head - _count + _buffer.Length) % _buffer.Length;
            for (int i = 0; i < _count; i++)
            {
                result[i] = _buffer[(start + i) % _buffer.Length];
            }
            return result;
        }
    }

    public T[] ReadLast(int count)
    {
        lock (_lock)
        {
            count = Math.Min(count, _count);
            var result = new T[count];
            if (count == 0) return result;

            int start = (_head - count + _buffer.Length) % _buffer.Length;
            for (int i = 0; i < count; i++)
            {
                result[i] = _buffer[(start + i) % _buffer.Length];
            }
            return result;
        }
    }

    public T[] ReadRange(long fromTicks, long toTicks, Func<T, long> ticksSelector)
    {
        lock (_lock)
        {
            var all = ReadAll();
            return all.Where(item => ticksSelector(item) >= fromTicks && ticksSelector(item) <= toTicks).ToArray();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _head = 0;
            _count = 0;
        }
    }
}
