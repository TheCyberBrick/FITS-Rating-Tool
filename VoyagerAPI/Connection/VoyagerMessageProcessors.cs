/*
    FITS Rating Tool
    Copyright (C) 2022 TheCyberBrick
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;

namespace VoyagerAPI.Connection
{
    public class VoyagerMessageProcessors
    {
        public static Action<string, JObject> WithMAC(string secret, params string[] fields)
        {
            return WithMAC(secret, (_, token) => token.ToString(), fields);
        }

        public static Action<string, JObject> WithMAC(string secret, Func<string, JToken, string> formatter, params string[] fields)
        {
            return (guid, args) =>
            {
                using (MD5 md5 = MD5.Create())
                {
                    StringBuilder str = new(secret);
                    str.Append(guid);
                    if (fields != null)
                    {
                        foreach (string field in fields)
                        {
                            args.TryGetValue(field, out JToken? token);
                            if (token != null) str.Append(formatter.Invoke(field, token));
                        }
                    }
                    byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(str.ToString()));
                    args.Add(new JProperty("MAC", BitConverter.ToString(hash).Replace("-", "").ToLower()));
                }
            };
        }
    }
}
