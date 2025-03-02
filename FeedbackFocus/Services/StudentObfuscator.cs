using System;

namespace FeedbackFocus.Services
{
    public class StudentObfuscator
    {
        List<string> firstNames = new List<string>
{
    "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda",
    "William", "Elizabeth", "David", "Barbara", "Richard", "Susan", "Joseph", "Jessica",
    "Thomas", "Sarah", "Charles", "Karen", "Christopher", "Nancy", "Daniel", "Lisa",
    "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra", "Donald", "Ashley",
    "Steven", "Kimberly", "Paul", "Emily", "Andrew", "Donna", "Joshua", "Michelle",
    "Kenneth", "Dorothy", "Kevin", "Carol", "Brian", "Amanda", "George", "Melissa",
    "Edward", "Deborah", "Ronald", "Stephanie", "Timothy", "Rebecca", "Jason", "Sharon",
    "Jeffrey", "Laura", "Ryan", "Cynthia", "Jacob", "Kathleen", "Gary", "Amy",
    "Nicholas", "Shirley", "Eric", "Angela", "Stephen", "Helen", "Jonathan", "Anna",
    "Larry", "Brenda", "Justin", "Pamela", "Scott", "Nicole", "Brandon", "Samantha",
    "Benjamin", "Katherine", "Samuel", "Emma", "Gregory", "Ruth", "Frank", "Christine",
    "Alexander", "Catherine", "Raymond", "Debra", "Patrick", "Rachel", "Jack", "Carolyn",
    "Dennis", "Janet", "Jerry", "Maria", "Tyler", "Heather", "Aaron", "Diane"
};
        List<string> lastNames = new List<string>
{
    "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
    "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas",
    "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White",
    "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker", "Young",
    "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
    "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell",
    "Carter", "Roberts", "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker",
    "Cruz", "Edwards", "Collins", "Reyes", "Stewart", "Morris", "Morales", "Murphy",
    "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper", "Peterson", "Bailey",
    "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson",
    "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza",
    "Ruiz", "Hughes", "Price", "Alvarez", "Castillo", "Sanders", "Patel", "Myers",
    "Long", "Ross", "Foster", "Jimenez"
};
        Dictionary<string, (string, string, string)> personInfo = new Dictionary<string, (string, string, string)>();

        private FeedbackService feedbackService;
        public StudentObfuscator(FeedbackService srvc)
        {
            feedbackService = srvc;
        }
        Random random = new Random();
        public async Task<bool> Obfuscate()
        {
            // This list will help check how many unique entries are in personInfo after the loop
            HashSet<string> originalUsernames = new HashSet<string>();

            foreach (var feedbackItem in await feedbackService.GetFeedback())
            {
                // Convert username to lower case to handle case sensitivity
                var originalUsername = feedbackItem.Username.ToLower();

                if (personInfo.ContainsKey(originalUsername))
                {
                    // Use existing data from the dictionary using the original username
                    var info = personInfo[originalUsername];
                    feedbackItem.FirstName = info.Item1;
                    feedbackItem.LastName = info.Item2;
                    feedbackItem.Username = info.Item3;
                }
                else
                {
                    // Generate random first name, last name, and username
                    string randomFirstName;
                    string randomLastName;
                    string randomUsername;

                    do
                    {
                        randomFirstName = firstNames[random.Next(firstNames.Count)];
                        randomLastName = lastNames[random.Next(lastNames.Count)];
                        randomUsername = randomFirstName + randomLastName;
                    } while (personInfo.Values.Any(info => info.Item3.Equals(randomUsername, StringComparison.OrdinalIgnoreCase)));

                    // Update feedback item
                    feedbackItem.FirstName = randomFirstName;
                    feedbackItem.LastName = randomLastName;
                    feedbackItem.Username = randomUsername;

                    // Add to dictionary for future use using the original username (lowercased)
                    personInfo[originalUsername] = (randomFirstName, randomLastName, randomUsername);
                }

                // Debugging: Add original username to HashSet to count unique entries
                originalUsernames.Add(originalUsername);

                // Debugging: Check how many unique entries in personInfo at this point
                Console.WriteLine($"Total unique usernames in personInfo: {personInfo.Count}");

                await feedbackService.SaveFeedback(feedbackItem);
            }

            // Final check to confirm uniqueness
            Console.WriteLine($"Total unique original usernames processed: {originalUsernames.Count}");
            Console.WriteLine($"Total unique usernames stored in personInfo: {personInfo.Count}");

            return true;
        }

    }
}
