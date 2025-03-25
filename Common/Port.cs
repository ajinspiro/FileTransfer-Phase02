namespace Common
{
    public class Port
    {
        private int _value;
        public Port(int value)
        {
            if (value < 1024 || value > 49151) throw new Exception("Port must be between 1024–49151");
            _value = value;
        }
        public static implicit operator Port(int integer) => new(integer);
        public static implicit operator int(Port port) => port._value;
    }
}
