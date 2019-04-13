using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainClientTestModule : GrpcCrossChainTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            
            Configure<GrpcCrossChainConfigOption>(option =>
            {
                option.LocalClient = true;
                option.LocalServer = true;
            });

            var services = context.Services;
            services.AddSingleton(provider =>
            {
                var mockCertificateStore = new Mock<ICertificateStore>();
                mockCertificateStore.Setup(m => m.LoadKeyStore(It.IsAny<string>()))
                    .Returns<string>((name) => { return @"-----BEGIN RSA PRIVATE KEY-----
MIIEjwIBAAKB/gm+Ca6sg/U+/IamfEx9FK/0brAyPWZV4MS9WRA5xx7UZaZ4iNon
LE8TT3E4skTLtkepAj1PSUztNPCD/Wq7gVvOxQYgwgmD7NUPkuLyRvNn1H8klcBm
MDYXWWdVIBbZK+avYqGtb+DgXVFhVsRFxRv8ELZXgOPLIAh/KXwHKsIRGQyZVyBu
ANeTumYtuqWv42JNTbybAS5B7JYPs2rj8nkTKxYrCPHeYNKVtGc3yHtb/BOrnRVA
QkYW0c0w4qZh/iMmLsnN7o3PSprsB0bzLLwDVREySKZyppF3Cu+1f0/YDD1J6Fvi
EtZUDkzklVwv0LgUQgn1kdhRIbbXAHG5AgMBAAECgf4BJKpJM5hWjpoeiOncJe9o
CHR2u6aF3D0AiUGqzEToAr+c7dcNVnx2GQuA+0i8FvWgah5HqIau/sTwFmUGAdPX
vKdVJUHv0OnZRLcVZ53Y0U3Xz6i9B6DPsCS/IfeehsSGkP0vgMU4q8t3kccXSCKt
uoIRi6ol0P/Ez3tEQSkMyjFBq7px3VmsdCVIhkxQvaeTS7JQQ8i75Xn8Ze+KAAs2
/9KvcLXO/VdKZLQxCIsFiilHpIbgxs6Fx5xq+qt0aOu0D9xvyvinsPGj7cKsInMF
0zRiddtTL2eQOtk7yqfrpKmIU6DwXjkvQ9IuNB4RsLAZ4frez820Rj7KWBiM+wJ/
MkpShMo69ZgHdksGim3fbelSJrdZcU/TcM41rtuT2YbgdQoWB1+eXnRsUQVZN4H+
BJWw/ad0RYEqkGj3zTJqaUc6pgZU+H6GMbXQFOt6HnvYWiz2gb9pF5TxNYqS33gO
rvInJGJdZAEV5ig0+caJP5LLvCqtMU8OzfhpLNOHcwJ/MZfBr+JX1mV1GG8aA4HM
91oEUxKvdI6hKea07pWzAP4gsnHqGzKyx/cxjqWChG0Zyb/nJ6R2iAjXA6X686kT
IavfteJ2ryzMsVwbf/2x3RdGBMqCkqaZH2RkRi34ehjSD0gGKb37/Y9Y1l/cBcMQ
Ka/ddjVUDlvqt/hmjRofIwJ/JhCVVwdPCzeQZlwxTjQNyr6wvLdIzviR3S9n+Lsg
tKRfXpdMxzX7xBixJ745okcVQtkex0+pNTaoRff9oGZJnvgYDzR5ukDiR9wK7Nqz
a0FoKBEiYGDGJeBJlrIVq2nPC2IkeGivsZMUxUmnl2tL1T/CT+Gph8oENaiRyyks
uwJ/H5GQ09trOqj+7vzaPF1GEjaVBiSg17trT/byOeXFOt6KBc2JzqJpN+1c+IbX
HGEux0SHaq7AXTvzUvk3VB/Oc+Kq12c/Uadc7ZHKV6EwtaJ5Cde3Yo72bgtD4YCl
6WMfZGbetXegjvnO/TesIWbYRREUEolD5pgQQ+e1sCBurQJ/FiWJc793qHRRklp9
fuyKG1W+m9jRgY8jc074LY6qU8YhX+DxGJufhH+rjRrgA4ZZYNdrI164V7hH4nTJ
rlfzGF7a+YwQr6GuaaoaT/te2hXrLSmAqybPEVsprP9unyDklxM3n2urbNeTEcR+
p1LVsMzwxFxt+Whdyxgd4qzrBg==
-----END RSA PRIVATE KEY-----"; });
                mockCertificateStore.Setup(m => m.LoadCertificate(It.IsAny<string>()))
                    .Returns<string>((name) => { return @"-----BEGIN CERTIFICATE-----
MIICtzCCAaKgAwIBAgIIWTIRLVpy1xEwDQYJKoZIhvcNAQELBQAwDzENMAsGA1UE
AwwEYWVsZjAeFw0xOTAzMjEwMDAwMDBaFw0yMDAzMjAwMDAwMDBaMA8xDTALBgNV
BAMMBGFlbGYwggEeMA0GCSqGSIb3DQEBAQUAA4IBCwAwggEGAoH+Cb4JrqyD9T78
hqZ8TH0Ur/RusDI9ZlXgxL1ZEDnHHtRlpniI2icsTxNPcTiyRMu2R6kCPU9JTO00
8IP9aruBW87FBiDCCYPs1Q+S4vJG82fUfySVwGYwNhdZZ1UgFtkr5q9ioa1v4OBd
UWFWxEXFG/wQtleA48sgCH8pfAcqwhEZDJlXIG4A15O6Zi26pa/jYk1NvJsBLkHs
lg+zauPyeRMrFisI8d5g0pW0ZzfIe1v8E6udFUBCRhbRzTDipmH+IyYuyc3ujc9K
muwHRvMsvANVETJIpnKmkXcK77V/T9gMPUnoW+IS1lQOTOSVXC/QuBRCCfWR2FEh
ttcAcbkCAwEAAaMeMBwwGgYDVR0RBBMwEYcEfwAAAYIJbG9jYWxob3N0MA0GCSqG
SIb3DQEBCwUAA4H/AAWzMLb2TPGvoykwD4Wpq96eoRzixB2ZMFDGSZ+vALZwp8JA
J5mDcNmPdHIwKp1rq0+Fy9SLIaJcQuoT8GChsKvZgf5CyA4pYtxs9d2af8VqKr9e
A2DAeWE1wDkQ5/Xs3bxMVNOM7gcULPWIaQqlBXW+gjQj87sByJq/ghDRq/0qPM99
ygxcat8262abGL4STuWpAY2iTri7WPQnytnAyg305KjIHCazIi/787vuAnZp58CP
9yL+1ak0ClAfId+qgMpZVYZcbuN//VL+8FSdlE/PqP3B4Dc/k9p9LGGSyRlNZbYW
eAkW/Qv4MEnbgaq97yC2lPkyrd19N2fh5oBT
-----END CERTIFICATE-----"; });
                return mockCertificateStore.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<ICrossChainDataProducer>();
                mockService.Setup(m=>m.GetChainHeightNeeded(It.IsAny<int>()))
                    .Returns(15);
                
                return mockService.Object;
            });

            services.AddSingleton<ICrossChainServer, CrossChainGrpcServer>();
        }
    }
}