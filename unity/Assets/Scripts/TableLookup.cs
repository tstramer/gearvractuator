using UnityEngine;
using System.Collections;

public class TableLookup {

	// Linearly interpolates a 2d lookup table to be able to lookup non-sampled data
	public static float lookup(float val, float[] _in, float[] _out) {
		int size = _in.Length;

		// take care the value is within range
		// val = constrain(val, _in[0], _in[size-1]);
		if (val <= _in[0]) return _out[0];
		if (val >= _in[size-1]) return _out[size-1];

		// search right interval
		int pos = 1;  // _in[0] allready tested
		while(val > _in[pos]) pos++;

		// this will handle all exact "points" in the _in array
		if (val == _in[pos]) return _out[pos];

		// interpolate in the right segment for the rest
		return (val - _in[pos-1]) * (_out[pos] - _out[pos-1]) / (_in[pos] - _in[pos-1]) + _out[pos-1];
	}
}