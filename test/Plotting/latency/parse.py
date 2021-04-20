import pandas as pd
import matplotlib.pyplot as plt
import numpy as np

for c in [3]:

    if c == 3: 
        points=[1,10,15,20,21,22,23,24,25,26]
    else: 
        points=[1]

    boxes = []
    for p in points:

        df = pd.read_csv(str(c)+'_'+str(p)+'.csv')
        col = 'ptime'
        df = df.loc[:, ~df.columns.str.contains('^Unnamed')].sort_values(by=col).reset_index(drop=True)

        _mx = 200 if p < 23 else 300 
        df = df[df[col]/1000 < _mx].sort_values(by='time')
        avg = sum(df.ptime)/len(df)
        boxes.append(list(df[col]))


    boxes = [np.array(box)/1000 for box in boxes]
    plt.violinplot(boxes, showmeans=True)

    plt.grid(True)
    averages = np.array([sum(box)/len(box) for box in boxes])
    plt.scatter(range(1, len(points)+1), averages, marker='H')
    plt.plot(range(1, len(points)+1), averages, marker='H')
    plt.xticks(range(1, len(points)+1), [str(_p) for _p in points])
    plt.show()