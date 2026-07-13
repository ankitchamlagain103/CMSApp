using System.Net;

namespace Infrastructure.Common
{
    // Shared by AuthorizedAction (per-request enforcement) and AuthService (login/refresh
    // enforcement) so both sides interpret ApplicationUser.UserIpAllowed identically.
    //
    // NOTE: the remote address comes from HttpContext.Connection.RemoteIpAddress, which is the
    // direct TCP peer. Behind a reverse proxy/load balancer that is the proxy's address, not the
    // client's -- configure ForwardedHeadersMiddleware (with KnownProxies) before trusting this
    // in such a deployment. See Docs/user_ip_restriction_implementation_guide.md.
    public static class IpAllowlistChecker
    {
        // Returns true only when the remote address matches an entry in the comma-separated
        // allowlist. Fails closed: an unknown remote address or an empty allowlist denies access
        // (an admin who enables IsIpRestricted without entries has restricted the user to nowhere,
        // which is safer than silently not restricting at all).
        public static bool IsAllowed(IPAddress remoteIpAddress, string allowedIpList)
        {
            if (remoteIpAddress == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(allowedIpList))
            {
                return false;
            }

            var normalizedRemoteAddress = NormalizeAddress(remoteIpAddress);

            var allowedTokens = allowedIpList.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var allowedToken in allowedTokens)
            {
                var trimmedToken = allowedToken.Trim();
                if (trimmedToken.Length == 0)
                {
                    continue;
                }

                if (!IPAddress.TryParse(trimmedToken, out var allowedAddress))
                {
                    continue;
                }

                var normalizedAllowedAddress = NormalizeAddress(allowedAddress);
                if (normalizedRemoteAddress.Equals(normalizedAllowedAddress))
                {
                    return true;
                }

                // "127.0.0.1" in the allowlist should also match a local "::1" connection (and
                // vice versa) -- Kestrel commonly reports loopback as IPv6 even when the admin
                // typed the IPv4 form.
                if (IPAddress.IsLoopback(normalizedRemoteAddress) && IPAddress.IsLoopback(normalizedAllowedAddress))
                {
                    return true;
                }
            }

            return false;
        }

        // Kestrel reports IPv4 clients on a dual-stack socket as IPv4-mapped IPv6 addresses
        // ("::ffff:203.0.113.10"), which would never equal the plain IPv4 form an admin types.
        private static IPAddress NormalizeAddress(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                return address.MapToIPv4();
            }

            return address;
        }
    }
}
