import matplotlib.pyplot as plt
import pandas as pd
import numpy as np


def calc(steps, mn, mx):
    step_n = (mx-_mn)/steps
    rng = list(range(int(mn), int(mx), int(step_n)))
    rng[-1] = mx
    return rng

amount = 20
cluster = 'timeout2'
data = 'y'
l = 0
boxes = []
_min = 900
_max = 0
labels = []
_range = [1, 5, 20, 15, 20]
# for i in range(1, amount+1):
# for i in [1, 5, 10, 15, 20]:
for i in _range:#, 40, 45, 50]:
    fn = str(cluster)+'/lat/log_'+str(cluster)+'_'+str(i)+'.csv'
    print('Loading', '"'+fn+'"')
    df = pd.read_csv(fn)[1000:-1000]
    # boxes.update({str(i):np.array(df.y)})
    boxes.append(df[data])
    _mn = min(df[data])
    _mx = max(df[data])
    if (_mn < _min): _min = _mn
    if (_mx > _max): _max = _mx


labels = calc(10, _min, _max)
    # scater = plt.scatter(df.x, df.y)
    # scater.set_rasterized(True)
    # break
fig = plt.figure(figsize=(10, 5)) 
    
# bp=plt.boxplot(boxes, labels=list(range(1,amount+1)), showmeans=True, patch_artist=True)
# bp=plt.boxplot(boxes, labels=[1, 5, 10, 15, 20], showmeans=True, patch_artist=True)
bp=plt.boxplot(boxes, labels=_range, showmeans=True, patch_artist=True)
plt.setp(bp['medians'], color='blue')
plt.setp(bp['means'], color='green')
for patch in bp['boxes']:
    patch.set(facecolor='white')
plt.xlabel('Points at a time', fontsize=16)
plt.ylabel('Consumption interval [ms]', fontsize=16)
plt.yticks(labels)
plt.xlim([0.5, len(_range)+.5])
plt.hlines(_min, 0.5, len(_range)+.5, colors='r')
plt.hlines(_max, 0.5, len(_range)+.5, colors='r')
plt.gca().get_yticklabels()[0].set_color("r")
plt.gca().get_yticklabels()[-1].set_color("r")
plt.grid(True)
plt.show()