using System.Text.Json.Serialization;

public class Skill
{
    public int SkillId { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    [JsonIgnore]
    public SkillProficiency Proficiency { get; set; }//future scope

}