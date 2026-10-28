namespace BLDC_Demo.Models
{
    // Represents one row from MotorTypes.txt: "Name,Gear,"
    public class MotorTypeItem
    {
        public string Name { get; set; }
        public string Gear { get; set; }

        public MotorTypeItem() { }

        public MotorTypeItem(string name, string gear)
        {
            Name = name;
            Gear = gear;
        }

        // Fallback text if a control ever displays this without DisplayMemberPath set.
        public override string ToString() => Name;
    }
}