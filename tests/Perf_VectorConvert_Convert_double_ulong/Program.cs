﻿// See https://aka.ms/new-console-template for more information

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Numerics;


using System.Collections.Generic;
using System.Text;

// Recreating app from performance benchmark

namespace Profile
{
	public static class ValuesGenerator
    {
        private const int Seed = 12345; // we always use the same seed to have repeatable results!

        public static T GetNonDefaultValue<T>()
        {
            if (typeof(T) == typeof(byte)) // we can't use ArrayOfUniqueValues for byte
                return Array<T>(byte.MaxValue).First(value => !value.Equals(default));
            else
                return ArrayOfUniqueValues<T>(2).First(value => !value.Equals(default));
        }

        /// <summary>
        /// does not support byte because there are only 256 unique byte values
        /// </summary>
        public static T[] ArrayOfUniqueValues<T>(int count)
        {
            // allocate the array first to try to take advantage of memory randomization
            // as it's usually the first thing called from GlobalSetup method
            // which with MemoryRandomization enabled is the first method called right after allocation
            // of random-sized memory by BDN engine
            T[] result = new T[count];

            var random = new Random(Seed); 

            var uniqueValues = new HashSet<T>();

            while (uniqueValues.Count != count)
            {
                T value = GenerateValue<T>(random);

                if (!uniqueValues.Contains(value))
                    uniqueValues.Add(value);
            }

            uniqueValues.CopyTo(result);

            return result;
        }
        
        public static T[] Array<T>(int count)
        {
            var result = new T[count];

            var random = new Random(Seed); 

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
            {
                random.NextBytes(Unsafe.As<byte[]>(result));
            }
            else
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = GenerateValue<T>(random);
                }
            }

            return result;
        }

        public static Dictionary<TKey, TValue> Dictionary<TKey, TValue>(int count)
        {
            var dictionary = new Dictionary<TKey, TValue>();

            var random = new Random(Seed);

            while (dictionary.Count != count)
            {
                TKey key = GenerateValue<TKey>(random);

                if (!dictionary.ContainsKey(key))
                    dictionary.Add(key, GenerateValue<TValue>(random));
            }

            return dictionary;
        }

        public static string[] ArrayOfStrings(int count, int minLength, int maxLength)
        {
            var random = new Random(Seed);

            string[] strings = new string[count];
            for (int i = 0; i < strings.Length; i++)
            {
                strings[i] = GenerateRandomString(random, minLength, maxLength);
            }
            return strings;
        }

        private static T GenerateValue<T>(Random random)
        {
            if (typeof(T) == typeof(char))
                return (T)(object)(char)random.Next(char.MinValue, char.MaxValue);
            if (typeof(T) == typeof(short))
                return (T)(object)(short)random.Next(short.MaxValue);
            if (typeof(T) == typeof(ushort))
                return (T)(object)(ushort)random.Next(short.MaxValue);
            if (typeof(T) == typeof(int))
                return (T)(object)random.Next();
            if (typeof(T) == typeof(uint))
                return (T)(object)(uint)random.Next();
            if (typeof(T) == typeof(long))
                return (T)(object)(long)random.Next();
            if (typeof(T) == typeof(ulong))
                return (T)(object)(ulong)random.Next();
            if (typeof(T) == typeof(float))
                return (T)(object)(float)random.NextDouble();
            if (typeof(T) == typeof(double))
                return (T)(object)random.NextDouble();
            if (typeof(T) == typeof(bool))
                return (T)(object)(random.NextDouble() > 0.5);
            if (typeof(T) == typeof(string))
                return (T)(object)GenerateRandomString(random, 1, 50);
            if (typeof(T) == typeof(Guid))
                return (T)(object)GenerateRandomGuid(random);

            throw new NotImplementedException($"{typeof(T).Name} is not implemented");
        }

        private static string GenerateRandomString(Random random, int minLength, int maxLength)
        {
            var length = random.Next(minLength, maxLength);

            var builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                var rangeSelector = random.Next(0, 3);

                if (rangeSelector == 0)
                    builder.Append((char) random.Next('a', 'z'));
                else if (rangeSelector == 1)
                    builder.Append((char) random.Next('A', 'Z'));
                else
                    builder.Append((char) random.Next('0', '9'));
            }

            return builder.ToString();
        }

        private static Guid GenerateRandomGuid(Random random)
        {
            byte[] bytes = new byte[16];
            random.NextBytes(bytes);
            return new Guid(bytes);
        }
    }
	
    

	public class Perf_VectorConvert
    {
        private const int Iterations = 1000;

        // These arrays are used for the Narrow benchmarks, so they need 2 vectors per iteration
        private static readonly double[] s_valuesDouble = ValuesGenerator.Array<double>(Vector<double>.Count * 2 * Iterations);


        public Vector<ulong> Convert_double_ulong() => Convert<double, ulong>(s_valuesDouble);
		
		private static Vector<TTo> Convert<TFrom, TTo>(TFrom[] values) where TFrom : struct where TTo : struct
        {
            var input = Unsafe.As<Vector<TFrom>[]>(values);
            var accum = Vector<TTo>.Zero;

            ref Vector<TFrom> ptr = ref input[0];
            for (int i = Iterations; i >= 0; --i)
            {
                accum ^= ConvertVector<TFrom, TTo>(ptr);
                ptr = ref Unsafe.Add(ref ptr, 1);
            }

            return accum;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<TTo> ConvertVector<TFrom, TTo>(Vector<TFrom> value) where TFrom : struct where TTo : struct
        {
            if (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(int))
                return (Vector<TTo>)(object)Vector.ConvertToInt32((Vector<float>)(object)value);
            if (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(uint))
                return (Vector<TTo>)(object)Vector.ConvertToUInt32((Vector<float>)(object)value);
            if (typeof(TFrom) == typeof(double) && typeof(TTo) == typeof(long))
                return (Vector<TTo>)(object)Vector.ConvertToInt64((Vector<double>)(object)value);
            if (typeof(TFrom) == typeof(double) && typeof(TTo) == typeof(ulong))
                return (Vector<TTo>)(object)Vector.ConvertToUInt64((Vector<double>)(object)value);
            if (typeof(TFrom) == typeof(int) && typeof(TTo) == typeof(float))
                return (Vector<TTo>)(object)Vector.ConvertToSingle((Vector<int>)(object)value);
            if (typeof(TFrom) == typeof(uint) && typeof(TTo) == typeof(float))
                return (Vector<TTo>)(object)Vector.ConvertToSingle((Vector<uint>)(object)value);
            if (typeof(TFrom) == typeof(long) && typeof(TTo) == typeof(double))
                return (Vector<TTo>)(object)Vector.ConvertToDouble((Vector<long>)(object)value);
            if (typeof(TFrom) == typeof(ulong) && typeof(TTo) == typeof(double))
                return (Vector<TTo>)(object)Vector.ConvertToDouble((Vector<ulong>)(object)value);

            throw new NotSupportedException("Type combination unsupported for Vector.ConvertToXXX");
        }
	}


    class Program
    {
        static void Main(string[] args)
        {			
			var mc = new Perf_VectorConvert();
			Console.WriteLine(mc.Convert_double_ulong());
          
        }
    }
}
