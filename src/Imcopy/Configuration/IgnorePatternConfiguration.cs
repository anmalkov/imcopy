﻿namespace Imcopy.Configuration;

public class IgnorePatternConfiguration
{
    public string Name { get; set; } = "";
    public IEnumerable<string> Patterns { get; set; } = new List<string>();
}
