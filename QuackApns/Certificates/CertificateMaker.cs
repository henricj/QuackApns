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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Security.Cryptography;
using Security.Cryptography.X509Certificates;

namespace QuackApns.Certificates
{
    public static class CertificateMaker
    {
        static readonly Oid ServerOid = new Oid("1.3.6.1.5.5.7.3.1");
        static readonly Oid ClientOid = new Oid("1.3.6.1.5.5.7.3.2");

        public static byte[] CreateCertificateP12(string commonName, bool isClient = false)
        {
            var cngParameters = new CngKeyCreationParameters();

            cngParameters.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));

            cngParameters.KeyUsage = CngKeyUsages.KeyAgreement | CngKeyUsages.Signing;
            cngParameters.ExportPolicy = CngExportPolicies.AllowPlaintextExport;

            var key = CngKey.Create(CngAlgorithm2.Rsa, null, cngParameters);

            var subjectName = new X500DistinguishedName("CN=" + commonName);

            var certParams = new X509CertificateCreationParameters(subjectName)
            {
                TakeOwnershipOfKey = true,
                SignatureAlgorithm = X509CertificateSignatureAlgorithm.RsaSha256
            };

            var enhancedKeyUsages = new OidCollection { isClient ? ClientOid : ServerOid };

            certParams.Extensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsages, false));

            var certificate = key.CreateSelfSignedCertificate(certParams);

            return certificate.Export(X509ContentType.Pkcs12, "");
        }
    }
}
