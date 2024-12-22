// Natural string comparison for proper file sorting
public class NaturalStringComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        int xPos = 0, yPos = 0;
        while (xPos < x.Length && yPos < y.Length)
        {
            if (char.IsDigit(x[xPos]) && char.IsDigit(y[yPos]))
            {
                int xNum = 0, yNum = 0;
                while (xPos < x.Length && char.IsDigit(x[xPos]))
                {
                    xNum = xNum * 10 + (x[xPos] - '0');
                    xPos++;
                }
                while (yPos < y.Length && char.IsDigit(y[yPos]))
                {
                    yNum = yNum * 10 + (y[yPos] - '0');
                    yPos++;
                }
                if (xNum != yNum) return xNum.CompareTo(yNum);
            }
            else
            {
                if (xPos >= x.Length) return -1;
                if (yPos >= y.Length) return 1;
                if (x[xPos] != y[yPos]) return x[xPos].CompareTo(y[yPos]);
                xPos++;
                yPos++;
            }
        }
        return x.Length.CompareTo(y.Length);
    }
}