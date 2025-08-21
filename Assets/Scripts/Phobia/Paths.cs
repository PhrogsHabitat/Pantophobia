// We don't use a namespace here because its a CORE utility class
// If it was in Phobia.utils, it would be in a namespace
// However, currently, its imported in literally EVERYTHING, since C# is a cunt
// and doesn't support global usings
// So we just use a static class here

public static class Paths
{
	public static string levelImage(string level, string image)
	{
		return $"Images/{level}/{image}";
	}

	public static string levelSound(string level, string sound)
	{
		return $"Sounds/{level}/{sound}";
	}

	public static string levelMusic(string level, string song)
	{
		return $"Music/{level}/{song}";
	}

	public static string levelData(string level, string file)
	{
		return $"Data/{level}/{file}";
	}

	public static string levelModel(string level, string model)
	{
		return $"Models/{level}/{model}";
	}

	public static string levelMat(string level, string mat)
	{
		return $"Mats/{level}/{mat}";
	}
	public static string levelFont(string level, string font)
	{
		return $"Fonts/{level}/{font}";
	}
}
