using UnityEngine;

public class AttenuationInterpolator {

	public void Init(float interpolationFrames)
    {
        numInterpFrames = interpolationFrames;
        startValue = .0f;
        endValue = .0f;
        currentValue = .0f;
        nextValue = .0f;
        frameIndex = .0f;
        isInit = true;
        isDone = false;
	}
	
    public void Reset()
    {
        isInit = true;
    }

	public float Update(out float perSampleIncrement, int samplesToInterpolate)
    {
        if (isDone)
        {
            perSampleIncrement = .0f;
            return currentValue;
        }
        else
        {
            float delta = 1.0f / numInterpFrames;
            float alpha = frameIndex * delta;
            if (alpha >= 1.0f)
            {
                isDone = true;
                currentValue = endValue;
                nextValue = endValue;
            }
            else if ((alpha + delta) >= 1.0f)
            {
                currentValue = Mathf.Lerp(startValue, endValue, alpha);
                nextValue = endValue;
            }
            else
            {
                currentValue = Mathf.Lerp(startValue, endValue, alpha);
                nextValue = Mathf.Lerp(startValue, endValue, alpha + delta);
            }

            perSampleIncrement = (nextValue - currentValue) / samplesToInterpolate;
            frameIndex += 1.0f;
            return currentValue;
        }
    }

    public void Set(float value)
    {
        if (isInit || numInterpFrames == .0f)
        {
            isInit = false;
            currentValue = value;
            startValue = value;
            endValue = value;
            frameIndex = numInterpFrames;
            if (numInterpFrames == .0f)
                isDone = true;
            else
                isDone = false;
        }
        else
        {
            startValue = nextValue;
            endValue = value;
            frameIndex = .0f;
            isDone = false;
        }
    }

    public float Get()
    {
        return currentValue;
    }

    float frameIndex;
    float numInterpFrames;

    float currentValue;
    float nextValue;
    float startValue;
    float endValue;

    bool isDone;
    bool isInit;
}
