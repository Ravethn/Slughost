namespace SlughostMod;

public class MyModdedEnums
{
    
    public class CreatureTemplateType
    {
        public static CreatureTemplate.Type SlugcatGhost;

        public static void RegisterValues()
        {
            string entryName = "Slughost";
            int conflictIndex = 1;
            while (CreatureTemplate.Type.values.entries.Contains(entryName))
            {
                conflictIndex += 1;
                entryName = "Slughost" + conflictIndex.ToString();
            }
            SlugcatGhost = new CreatureTemplate.Type(entryName, true);

        }

        public static void UnregisterValues()
        {
            if (SlugcatGhost != null)
            {
                SlugcatGhost.Unregister();
                SlugcatGhost = null;
            }
        }
    }
}
