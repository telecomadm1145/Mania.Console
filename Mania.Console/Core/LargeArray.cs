using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BeatmapEditor.Performance
{
    // 手动释放的超大数组w
    public unsafe class LargeArray<T> : IDisposable, IEnumerable<T> where T : struct
    {
        private T* data;
        private long size;
        public LargeArray(T* data, long size)
        {
            this.data = data;
            this.size = size;
        }
        public LargeArray(long size)
        {
            this.size = size;
            this.data = (T*)Marshal.AllocHGlobal((nint)(size * sizeof(T)));
        }
        public T* GetPointer()
        {
            return data;
        }
        public T this[long index]
        {
            get { if (index < Length && index >= 0) return data[index]; throw new IndexOutOfRangeException(); }
            set { if (index < Length && index >= 0) data[index] = value; else throw new IndexOutOfRangeException(); }
        }
        public long Length
        {
            get
            {
                return size;
            }
        }

        ~LargeArray()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (data != null)
            {
                Marshal.FreeHGlobal((nint)data);
                data = null;
                size = 0;
            }
            GC.SuppressFinalize(this);
        }
        private class LargeArrayEnumerator : IEnumerator<T>, IEnumerator
        {
            private LargeArray<T> arr;
            private long index;
            public LargeArrayEnumerator(LargeArray<T> arr)
            {
                this.arr = arr;
            }
            public T Current => index == -1 ? default : arr[index];
            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
                // Do nothing since it has nothing to do with the large array's memory
            }

            public bool MoveNext()
            {
                if (index == -1)
                    return false;
                index++;
                if (index >= arr.Length)
                {
                    index = -1;
                    return false;
                }
                return true;
            }

            public void Reset()
            {
                index = 0;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new LargeArrayEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new LargeArrayEnumerator(this);
        }
    }
}
