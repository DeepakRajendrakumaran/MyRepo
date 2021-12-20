// See https://aka.ms/new-console-template for more information

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Buffers;
using System.Numerics;


using System.Collections.Generic;
using System.Text;

// Recreating app from performance benchmark

namespace Profile
{
		
    public class RentReturnArrayPoolTests<T>
    {
        private readonly ArrayPool<T> _createdPool = ArrayPool<T>.Create();
        private readonly T[][] _nestedArrays = new T[NestedDepth][];
        private const int Iterations = 10_000_000;
        private const int NestedDepth = 8;
        public int RentalSize = 4096;
        public bool ManipulateArray = false;
        public bool Async = false;
        public bool UseSharedPool = false;
        private ArrayPool<T> Pool => UseSharedPool ? ArrayPool<T>.Shared : _createdPool;
        private static void Clear(T[] arr) => arr.AsSpan().Clear();


        private static T IterateAll(T[] arr)
        {
            T ret = default;
            foreach (T item in arr)
            {
                ret = item;
            }
            return ret;
        }

        public async Task SingleSerial()
        {
            ArrayPool<T> pool = Pool;
            for (int i = 0; i < Iterations; i++)
            {
                T[] arr = pool.Rent(RentalSize);
                if (ManipulateArray) Clear(arr);
                if (Async) await Task.Yield();
                if (ManipulateArray) IterateAll(arr);
                pool.Return(arr);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task SingleParallel()
        {
            ArrayPool<T> pool = Pool;
            //Console.WriteLine("\n Environment.ProcessorCount = " + Environment.ProcessorCount + "\n");
            await Task.WhenAll(
                Enumerable
                .Range(0, Environment.ProcessorCount)
                .Select(_ =>
                Task.Run(async delegate
                {
                    for (int i = 0; i < Iterations; i++)
                    {
                        T[] arr = pool.Rent(RentalSize);
                        if (ManipulateArray) Clear(arr);
                        if (Async) await Task.Yield();
                        if (ManipulateArray) IterateAll(arr);
                        pool.Return(arr);
                    }
                })));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task SingleParallelMwe()
        {
            SpinLock slock = new SpinLock(false);
            await Task.WhenAll(
                Enumerable
                .Range(0, 48)
                .Select(_ =>
                Task.Run(async delegate
                {
                    for (int i = 0; i < Iterations; i++)
                    {				
                        bool lockTaken = false;
                        try
                        {
                            slock.Enter(ref lockTaken);
							
                        }
                        finally
                        {
                            if (lockTaken) slock.Exit(false);
                        }
                    }
                })));

        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task ProducerConsumer()
        {
            ArrayPool<T> pool = Pool;
            Channel<T[]> buffers = Channel.CreateBounded<T[]>(1);
            Task consumer = Task.Run(async delegate
            {
                ChannelReader<T[]> reader = buffers.Reader;
                while (true)
                {
                    ValueTask<bool> read = reader.WaitToReadAsync();
                    if (!(Async ? await read : read.AsTask().Result))
                    {
                        break;
                    }
                    
                    while (reader.TryRead(out T[] buffer))
                    {
                        if (ManipulateArray) IterateAll(buffer);
                        pool.Return(buffer);
                    }
                }
            });
            
            ChannelWriter<T[]> writer = buffers.Writer;
            for (int i = 0; i < Iterations; i++)
            {
                T[] buffer = pool.Rent(RentalSize);
                if (ManipulateArray) IterateAll(buffer);
                ValueTask write = writer.WriteAsync(buffer);
                if (Async)
                {
                    await write;
                }
                else
                {
                    write.AsTask().Wait();
                }
            }
            writer.Complete();
            
            
            await consumer;
        }
    }


	
    class Program
    {
        static void Main(string[] args)
        {
            var mc = new RentReturnArrayPoolTests<object>();
			// Modify this call to test different functionality
            mc.SingleParallelMwe().GetAwaiter().GetResult();
			
			
          
        }
    }
}
