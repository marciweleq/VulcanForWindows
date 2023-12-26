using System;

namespace VulcanForWindows.Classes;

public class Column
{
    public int Id { get; set; }
    public Guid Key { get; set; }
    public int PeriodId { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string Group { get; set; }
    public int Number { get; set; }
    public uint Color { get; set; }
    public int Weight { get; set; }
    public string subjectName { get; set; }
}