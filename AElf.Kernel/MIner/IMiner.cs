﻿namespace AElf.Kernel.MIner
{
    public interface IMiner
    {
        void Start();
        void Stop();
        Hash ChainId { get; }
    }
}