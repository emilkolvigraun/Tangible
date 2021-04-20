import matplotlib.pyplot as plt
import pandas as pd
import numpy as np
from scipy.interpolate import make_interp_spline, BSpline
from scipy.ndimage.filters import gaussian_filter1d

window = 10
_range = .35
col='time_proc'

def gm(df, i, t0):
    outlier = False
    # start = i - window
    # if start < 0: start = 0
    # end = i + window
    # if end >= len(df): end = len(df)-1

    # rows = df.iloc[start:end][col]
    current = df.iloc[i]
    # average = sum(rows)/len(rows)

    # # if current[col] > average+(average*_range) or current[col] < average-(average*_range) or average > 100000 or average < 35000:
    if current[col] > 100000 or current[col] < 35000:
        outlier = True

    delta_time = float(current['time'])-float(t0)

    return outlier, current[col]/1000, delta_time


for c in [3, 4]:

    if c == 3: 
        points=[1,10,15,20]
    else: 
        points=[1]

    for p in points:
        df = pd.read_csv(str(c)+'_'+str(p)+'.csv').sort_values(by='time').reset_index(drop=True)

        T0 = df.iloc[0].time
        filtered = []
        time = []
        for i in range(len(df)):
            outlier, average, delta_time = gm(df, i, T0)

            if outlier: continue

            filtered.append(average)
            time.append(delta_time)
        
        Tdf = pd.DataFrame.from_dict({'time':time, 'filtered':filtered}).groupby('time', as_index=False).mean().sort_values(by='time').reset_index(drop=True)
        
        Tdf.to_csv(str(c)+'_'+str(p)+'_clean.csv')




