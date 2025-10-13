using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using BLTAdoptAHero.Powers;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=3rL4RHje}Class Config")]
    internal class GlobalHeroClassConfig : IUpdateFromDefault, IDocumentable
    {
        #region Static
        private const string ID = "Adopt A Hero - Class Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalHeroClassConfig));
        internal static GlobalHeroClassConfig Get() => ActionManager.GetGlobalConfig<GlobalHeroClassConfig>(ID);
        internal static GlobalHeroClassConfig Get(BannerlordTwitch.Settings fromSettings) => fromSettings.GetGlobalConfig<GlobalHeroClassConfig>(ID);
        
        #region Secret Bannerlord Class
        // Secret class "bannerlord" is defined in YAML but hidden from UI and restricted to FNC_Chair
        private const string SECRET_CLASS_NAME = "bannerlord";
        private const string SECRET_CLASS_USER = "fnc_chair"; // Lowercase for comparison
        #endregion
        #endregion

        #region User Editable
        // Internal storage for all class definitions (including secret classes)
        [Browsable(false)]
        public ObservableCollection<HeroClassDef> ClassDefs { get; set; } = new();

        // UI-visible property that filters out secret classes
        [LocDisplayName("{=462kHfn2}Class Definitions"),
         LocDescription("{=NFjlVt57}Defined classes"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(1), UsedImplicitly]
        public ObservableCollection<HeroClassDef> EditableClassDefs
        {
            get
            {
                // Filter out the secret bannerlord class for display
                var filtered = new ObservableCollection<HeroClassDef>(
                    ClassDefs.Where(c => !c.Name.ToString().Equals(SECRET_CLASS_NAME, StringComparison.InvariantCultureIgnoreCase))
                );
                return filtered;
            }
            set
            {
                // When setting, preserve the secret class if it exists
                var secretClass = ClassDefs.FirstOrDefault(c => 
                    c.Name.ToString().Equals(SECRET_CLASS_NAME, StringComparison.InvariantCultureIgnoreCase));
                
                ClassDefs.Clear();
                foreach (var classDef in value)
                {
                    ClassDefs.Add(classDef);
                }
                
                // Re-add secret class if it existed
                if (secretClass != null)
                {
                    ClassDefs.Add(secretClass);
                }
            }
        }

        [LocDisplayName("{=Q0yTbTCT}Class Level Requirements"),
         LocDescription("{=y8LLccGK}Requirements for class levels"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(2), UsedImplicitly]
        public ObservableCollection<ClassLevelRequirementsDef> ClassLevelRequirements { get; set; } = new();
        #endregion

        #region Public Interface
        [Browsable(false), YamlIgnore]
        public IEnumerable<HeroClassDef> ValidClasses => ClassDefs
            .Where(c => c.Enabled && !c.Name.ToString().Equals(SECRET_CLASS_NAME, StringComparison.InvariantCultureIgnoreCase));

        [Browsable(false), YamlIgnore]
        public IEnumerable<string> ClassNames => ValidClasses.Select(c => c.Name?.ToString().ToLower());

        public HeroClassDef GetClass(Guid id)
        {
            // Search in ClassDefs (not ValidClasses) to allow retrieving disabled/secret classes by ID
            return ClassDefs.FirstOrDefault(c => c.ID == id);
        }

        public HeroClassDef FindClass(string search)
            => FindClass(search, null);
        
        /// <summary>
        /// Find a class by name, with support for secret Bannerlord class
        /// </summary>
        /// <param name="search">Class name to search for</param>
        /// <param name="username">Username requesting the class (for secret class access control)</param>
        /// <returns>The matching class, or null if not found or unauthorized</returns>
        public HeroClassDef FindClass(string search, string username)
        {
            // Check for secret "Bannerlord" class
            if (!string.IsNullOrEmpty(search) && 
                search.Equals(SECRET_CLASS_NAME, StringComparison.InvariantCultureIgnoreCase))
            {
                // Only allow FNC_Chair to access this class
                if (string.IsNullOrEmpty(username) || 
                    !username.Equals(SECRET_CLASS_USER, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Return null - class doesn't "exist" for other users
                    return null;
                }
                
                // FNC_Chair authenticated - return the secret class from ClassDefs (ignore Enabled flag)
                return ClassDefs.FirstOrDefault(c => 
                    c.Name.ToString().Equals(SECRET_CLASS_NAME, StringComparison.InvariantCultureIgnoreCase));
            }
            
            // Normal class search (ValidClasses already filters out secret class)
            return ValidClasses.FirstOrDefault(c => c.Name.ToString().Equals(search, StringComparison.InvariantCultureIgnoreCase));
        }

        [Browsable(false), YamlIgnore]
        public IEnumerable<ClassLevelRequirementsDef> ValidClassLevelRequirements
            => ClassLevelRequirements
                .Where(c => c.Enabled && c.ClassLevel != 0)
                .OrderBy(c => c.ClassLevel);

        public int GetHeroClassLevel(Hero hero)
        {
            int level = 0;
            foreach (var requirements in ValidClassLevelRequirements)
            {
                if (!requirements.IsMet(hero)) return level;
                level = requirements.ClassLevel;
            }
            return level;
        }
        #endregion

        #region IUpdateFromDefault
        public void OnUpdateFromDefault(BannerlordTwitch.Settings defaultSettings)
        {
            ClassDefs ??= new();
            ClassLevelRequirements ??= new();

            SettingsHelpers.MergeCollections(
                ClassDefs,
                Get(defaultSettings).ClassDefs,
                (a, b) => a.ID == b.ID
            );
            SettingsHelpers.MergeCollections(
                ClassLevelRequirements,
                Get(defaultSettings).ClassLevelRequirements,
                (a, b) => a.ID == b.ID
            );
        }
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.Div("class-config", () =>
            {
                generator.H1("{=E0CBnj57}Classes".Translate());
                // foreach (var cl in ClassDefs)
                // {
                //     generator.LinkToAnchor(cl.Name, () => generator.H2(cl.Name));
                // }
                foreach (var cl in ValidClasses)
                {
                    generator.MakeAnchor(cl.Name.ToString(), () => generator.H2(cl.Name.ToString()));
                    cl.GenerateDocumentation(generator);
                    generator.Br();
                }
            });
        }
        #endregion
    }

    [LocDisplayName("{=uZUsZLGm}Class Level Requirements Definition")]
    public class ClassLevelRequirementsDef : INotifyPropertyChanged, ICloneable
    {
        #region User Editable
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get; set; } = Guid.NewGuid();

        [PropertyOrder(1), UsedImplicitly]
        public bool Enabled { get; set; }

        [LocDisplayName("{=0raqv788}ClassLevel"),
         LocDescription("{=el4y84iD}Class level"),
         PropertyOrder(2), UsedImplicitly]
        public int ClassLevel { get; set; }

        [LocDisplayName("{=aMGoiH53}Requirements"),
         LocDescription("{=G95DA4OM}Requirements for this class level"),
         PropertyOrder(3), UsedImplicitly,
         Editor(typeof(DerivedClassCollectionEditor<IAchievementRequirement>),
             typeof(DerivedClassCollectionEditor<IAchievementRequirement>))]
        public ObservableCollection<IAchievementRequirement> Requirements { get; set; } = new();
        #endregion

        #region Public Interface
        public override string ToString()
            => $"{ClassLevel}: "
               + (!Requirements.Any()
                   ? "{=7WLbMjpz}(no requirements)".Translate()
                   : string.Join(" + ", Requirements.Select(r => r.ToString())));

        public bool IsMet(Hero hero) => Requirements.All(r => r.IsMet(hero));
        #endregion

        #region ICloneable
        public object Clone() =>
            new ClassLevelRequirementsDef
            {
                Requirements = new(CloneHelpers.CloneCollection(Requirements)),
            };
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}