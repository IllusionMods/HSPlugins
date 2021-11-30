using System.Xml;

namespace HSUS.Features
{
    public interface IFeature
    {
        void Awake();
        void LevelLoaded();
        void LoadParams(XmlNode node);
        void SaveParams(XmlTextWriter writer);
    }
}
