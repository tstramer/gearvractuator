using System;
using System.Linq;

/**
 * Collection of helpers for converting between various primitive data types / formats
 */
public static class DataConversions {

	public static byte[] ToBytesLittleEndian(short num) {
		byte[] bytes = BitConverter.GetBytes(num);
		if (!BitConverter.IsLittleEndian) {
			Array.Reverse(bytes); //reverse it so we get little endian.
		}
		return bytes;
	}

	public static string ToReadableString(byte[] bytes) {
		return string.Join(",", bytes.Select(d => string.Format("{0:X2} ", d)).ToArray());
	}
}