using Verse;

namespace AmorTentaculum
{
    
    public class HediffSetConditional
    {
        public HediffDef hediff;
        public float severity = -1f;
        public List<HediffDef> hediffConditionals = new List<HediffDef>();
        public String hediffComparator;
        public List<String> genderConditionals = new List<String>();
        public String genderComparator;

    }
}
