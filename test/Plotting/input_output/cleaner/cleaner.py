import pandas as pd 
import numpy as np

# df = pd.read_csv('receiver_log1m.txt').sort_values(by='val').reset_index(drop=True)
# df.to_csv('log_write.csv', index = True)
# exit(0)

def calc(collection, _range, steps):
    _max_n = max(collection)
    _min_n = min(collection)
    _avg_n = sum(collection)/len(collection)
    step_n = (_max_n-_min_n)/steps
    rng = list(range(int(_min_n), int(_max_n), int(step_n)))
    labels = []
    _avg_i = 0
    for i in range(len(rng)-1):
        if (_avg_n-_range > rng[i]): labels.append(rng[i])
        if (_avg_n > rng[i] and _avg_n < rng[i+1]):
            labels.append(_avg_n)
            _avg_i = len(labels)-1
        if (_avg_n+_range < rng[i]): labels.append(rng[i])

    labels.append(rng[-1])
    return _avg_i, labels, _avg_n, _min_n, _max_n


df = pd.read_csv('log_write.csv')
x = []
y = []
h = []
start = df.iloc[0]['T0']
for i in range(1, len(df)):
    T0 = df.iloc[i]['T0']
    T1 = df.iloc[i-1]['T0']
    d = (T0-T1)/1000
    # if d>70: continue
    if (d==0): h.append(1000/1)
    else: h.append(1000/d)
    x.append((T0-start)/1000)
    y.append(d)

import matplotlib.pyplot as plt
from matplotlib import gridspec

print(min(y))


fig = plt.figure(figsize=(10, 5)) 
gs = gridspec.GridSpec(1, 2, width_ratios=[1, 3]) 
ax0 = plt.subplot(gs[1])
ax1 = plt.subplot(gs[0])


_h_avg_i, h_labels, h_avg, h_min, _max = calc(h, 20, 7)
vp = ax1.violinplot([h], showmeans=True)
vp['cmeans'].set_color('b')
ax1.hlines(h_min, 0, .9, colors=["r"])
ax1.hlines(h_avg, 0, .9, colors=["r"])
ax1.set_xlim([0.5,1.5])
ax1.grid(True)
ax1.set_ylabel('Consumption frequency [Hertz]')
plt.sca(ax1)
plt.yticks(h_labels)
plt.gca().get_yticklabels()[0].set_color("r")
plt.gca().get_yticklabels()[_h_avg_i].set_color("r")
plt.xticks([1], ['Requests'])

_min_x = min(x)
ax0.scatter(x, y, alpha=.15)
_y_avg_i, _y_labels, y_avg, y_min, y_max = calc(y, 3, 7, )
ax0.hlines((1/h_avg)*1000, _min_x, _min_x, colors=["r"])
ax0.yaxis.set_label_position("right")
ax0.yaxis.tick_right()
ax0.set_xlabel('Time since start [ms]')
ax0.set_ylabel('Consumption interval [ms]')
plt.sca(ax0)
plt.yticks(_y_labels)
plt.gca().get_yticklabels()[_y_avg_i].set_color("r")
plt.grid(True)
plt.xlim([_min_x, max(x)])

fig.tight_layout()
plt.show()