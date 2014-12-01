// Copyright (c) 2014 Henric Jungheim <software@henric.org>
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Data;
using QuackApns.Data;
using QuackApns.Utility;

namespace QuackApns.SqlServerRepository
{
    public class KeyDataReader : IDataReader
    {
        static readonly string[] Columns = { "Token", "UnixTimestamp", "ReceivedUtc" };
        static readonly Type[] Types = { typeof(byte[]), typeof(int), typeof(DateTime) };
        readonly KeyGenerator _keyGenerator;
        readonly DateTime _now;
        readonly int _rowCount;
        readonly int _unixNow;
        bool _keyRead;
        int _rowIndex;

        public KeyDataReader(KeyGenerator keyGenerator, int count)
        {
            if (null == keyGenerator)
                throw new ArgumentNullException("keyGenerator");
            if (count < 1)
                throw new ArgumentOutOfRangeException("count");

            _keyGenerator = keyGenerator;
            _rowCount = count;

            _now = DateTime.UtcNow;
            _unixNow = _now.ToInt32UnixEpoch();
        }

        #region IDataReader Members

        public void Dispose()
        { }

        public string GetName(int i)
        {
            return Columns[i];
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            return Types[i];
        }

        public object GetValue(int i)
        {
            switch (i)
            {
                case 0:
                    {
                        var bytes = new byte[ApnsConstants.DeviceTokenLength];

                        GetBytes(0, 0, bytes, 0, bytes.Length);

                        return bytes;
                    }
                case 1:
                    return GetInt32(1);
                case 2:
                    return GetDateTime(2);
            }

            throw new IndexOutOfRangeException();
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public int GetOrdinal(string name)
        {
            return Array.IndexOf(Columns, name);
        }

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            if (0 != i)
                throw new InvalidCastException();

            if (0 != fieldOffset)
                throw new NotSupportedException();

            if (ApnsConstants.DeviceTokenLength != length)
                throw new ArgumentOutOfRangeException("length");

            if (_keyRead)
                throw new InvalidOperationException("The key can only be read once");

            _keyGenerator.Next(buffer, bufferoffset, length);

            _keyRead = true;

            return ApnsConstants.DeviceTokenLength;
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            if (1 != i)
                throw new InvalidCastException();

            return _unixNow;
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            if (2 != i)
                throw new InvalidCastException();

            return _now;
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            return false;
        }

        public int FieldCount
        {
            get { return Columns.Length; }
        }

        object IDataRecord.this[int i]
        {
            get { throw new NotImplementedException(); }
        }

        object IDataRecord.this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        public void Close()
        {
            _rowIndex = _rowCount;
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            return false;
        }

        public bool Read()
        {
            if (_rowIndex >= _rowCount)
                return false;

            _keyRead = false;

            ++_rowIndex;

            return true;
        }

        public int Depth
        {
            get { return 0; }
        }

        public bool IsClosed
        {
            get { return false; }
        }

        public int RecordsAffected
        {
            get { return 0; }
        }

        #endregion
    }
}
