using System;

namespace AElf.WebApp.MessageQueue.Dtos;

public class SetStateInput
{
    public Guid Id { get; set; }
    public bool IsStop { get; set; }
}