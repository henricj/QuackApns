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
using System.IO;
using System.IO.IsolatedStorage;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace QuackApns.Certificates
{
    public static class IsolatedStorageCertificates
    {
        public static async Task<X509Certificate2> GetCertificateAsync(string commonName, string filename, bool isClient, CancellationToken cancellationToken)
        {
            using (var iso = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                byte[] pfx = null;

                if (iso.FileExists(filename))
                {
                    using (var file = iso.OpenFile(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        pfx = new byte[file.Length];

                        if (pfx.Length != await file.ReadAsync(pfx, 0, pfx.Length, cancellationToken).ConfigureAwait(false))
                        {
                            pfx = null;

                            file.Close();

                            iso.DeleteFile(filename);
                        }
                    }
                }

                if (null == pfx)
                {
                    pfx = CertificateMaker.CreateCertificateP12(commonName, isClient);

                    using (var file = iso.CreateFile(filename))
                    {
                        await file.WriteAsync(pfx, 0, pfx.Length, cancellationToken).ConfigureAwait(false);
                    }
                }

                var certificate = new X509Certificate2();

                try
                {
                    certificate.Import(pfx, "", X509KeyStorageFlags.Exportable);

                    return certificate;
                }
                catch (Exception)
                {
                    try
                    {
                        iso.DeleteFile(filename);
                    }
                    catch (Exception)
                    { }

                    throw;
                }
            }
        }
    }
}
