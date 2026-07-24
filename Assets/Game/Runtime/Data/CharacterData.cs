using System.Collections.Generic;

namespace Game.Runtime.Core
{
    public class CharacterData
    {
        public int id;
        public string name;
        public string species;
        public string description;

        public int homePlanet;
        public List<int> planetOption;

        public List<int> itemIds;

        public List<string> questions;
        public List<string> shortQuestions;
        public List<string> answers;

        public string glitterPrefab;

        public string profile;
        public string portrait;
        public string fullBody;
        public string xray;
    }
}