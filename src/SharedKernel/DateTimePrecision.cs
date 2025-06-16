namespace SharedKernel
{
    public enum DateTimePrecision
    {
        None,
        Year,        // 1990
        Month,       // 1990-04
        Day,         // 1990-04-25
        Hour,        // 1990-04-25T14
        Minute,      // 1990-04-25T14:30
        Second,      // 1990-04-25T14:30:00
        Millisecond, // 1990-04-25T14:30:00.123
        Microsecond  // 1990-04-25T14:30:00.123456

    }
}
