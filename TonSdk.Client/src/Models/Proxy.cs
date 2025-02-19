using Org.BouncyCastle.Crypto.Parameters;

namespace TonSdk.Client
{
    public partial class Proxy
    {
        public override bool Equals(object obj)
        {
            Proxy tmpProxy = obj as Proxy;
            if (tmpProxy == null) return false;

            if (Ip != tmpProxy.Ip) return false;
            if (Port != tmpProxy.Port) return false;
            if (UserName != tmpProxy.UserName) return false;
            if (Password != tmpProxy.Password) return false;
            if (ProxyType != tmpProxy.ProxyType) return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = Ip.GetHashCode();
            hashCode = 31 * hashCode + Port.GetHashCode();
            hashCode = 31 * hashCode + UserName.GetHashCode();
            hashCode = 31 * hashCode + Password.GetHashCode();
            hashCode = 31 * hashCode + ProxyType.GetHashCode();
            return hashCode;
        }
    }
}