using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Tracer
{
    class Program
    {
        static void Main(string[] args)
        {
            Ping pingSender = new Ping();
            var options = new PingOptions();

            options.DontFragment = true;
            options.Ttl = 1;
            PingReply reply;

            var destination = args[0];
            var isIpAdress = IsIPAddress(destination);
            var destHost = Dns.GetHostEntry(destination);

            Console.WriteLine("N   " + string.Join("",
                new string[]
                {
                    "IP".PadRight(21),
                    "AS".PadRight(21),
                    "Provider".PadRight(61),
                    "Country"
                }));

            while (true)
            {
                reply = pingSender.Send(destination, 120, new byte[] { }, options);
                var domain = GetDomainInfo(reply.Address.ToString());

                Console.WriteLine(options.Ttl.ToString().PadRight(4) + domain.ToString());

                if ((isIpAdress && reply.Address.Equals(IPAddress.Parse(destination))) ||
                    (!isIpAdress && destHost.AddressList.Contains(reply.Address)))
                    break;
                options.Ttl++;
            }
            Console.Write("Press any key");
            Console.ReadKey();
        }

        static Domain GetDomainInfo(string domainIP)
        {
            var domain = new Domain();
            domain.IP = domainIP;
            try
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    tcpClient.Connect("whois.ripe.net", 43);

                    byte[] domainQueryBytes = Encoding.ASCII.GetBytes(domainIP + "\r\n");

                    using (Stream stream = tcpClient.GetStream())
                    {
                        stream.Write(domainQueryBytes, 0, domainQueryBytes.Length);

                        using (StreamReader sr = new StreamReader(tcpClient.GetStream(), Encoding.UTF8))
                        {
                            string row;
                            while ((row = sr.ReadLine()) != null)
                            {
                                if (domain.Provider == "" && row.StartsWith("descr:"))
                                    domain.Provider = row.Substring(6).Trim();
                                if (row.StartsWith("origin:"))
                                    domain.AS = row.Substring(7).TrimStart();
                                if (row.StartsWith("country:"))
                                    domain.Country = row.Substring(8).TrimStart();
                            }
                        }
                    }
                }
            }
            catch { }
            return domain;
        }

        static bool IsIPAddress(string ipAddress)
        {
            try
            {
                IPAddress address;
                return IPAddress.TryParse(ipAddress, out address);
            }
            catch
            {
                return false;
            }
        }
    }

    public class Domain
    {
        public string IP { get; set; } = "";
        public string AS { get; set; } = "";
        public string Country { get; set; } = "";
        public string Provider { get; set; } = "";

        public override string ToString()
        {
            if (AS == "")
                return IP;

            var result = new StringBuilder();
            result.Append(IP.PadRight(20) + ' ');
            result.Append(AS.PadRight(20) + ' ');
            result.Append(Provider.PadRight(60) + ' ');
            result.Append(Country);

            return result.ToString();
        }
    }
}
