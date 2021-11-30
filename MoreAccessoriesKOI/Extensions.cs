namespace MoreAccessoriesKOI
{
    public static class Extensions
    {
        public static ChaFileCoordinate GetCoordinate(this ChaFile self, int index)
        {
#if KOIKATSU
            return self.coordinate[index];
#elif EMOTIONCREATORS
            return self.coordinate;
#endif
        }

        public static int GetCoordinateType(this ChaFileStatus self)
        {
#if KOIKATSU
            return self.coordinateType;
#elif EMOTIONCREATORS
            return 0;
#endif
        }
    }
}
