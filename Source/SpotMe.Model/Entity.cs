﻿namespace SpotMe.Model;

public class Entity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
}
