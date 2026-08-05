namespace ProjectManagement.API.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // This will store whether it is assigned to "Program", "Portfolio", "Project", etc.
        public string AssignTo { get; set; }
    }
}