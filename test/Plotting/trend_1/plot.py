import matplotlib.pyplot as plt
import pandas as pd

window = 10000
cluster = 6
_type = 'send'
_title = 'consumption'
df = pd.read_csv(str(cluster)+'_'+str(_type)+'.csv')

filtered = []
time = []
T0 = df.iloc[0].time
for i in range(len(df)):

    i_minus = i-int(window/2)
    if (i_minus<0): i_minus = 0

    i_plus = i+int(window/2)
    if (i_plus>len(df)-1): i_plus = len(df)-1

    df_i = df.iloc[i_minus:i_plus]
    filtered.append(
        sum(df_i.interval)/len(df_i.interval)
    )

    row = df.iloc[i]
    T1 = row.time - T0
    if (T1 == 0): time.append(0)
    else: time.append(((T1/1000)/1000)/60)
    
from matplotlib import gridspec
from matplotlib.backends.backend_pdf import PdfPages

fig = plt.figure(figsize=(12, 5)) 
gs = gridspec.GridSpec(1, 2, width_ratios=[1, 4]) 
ax0 = plt.subplot(gs[0])
ax1 = plt.subplot(gs[1])

vp = ax0.violinplot([df.interval], showmeans=True)
vp['cmeans'].set_color('#27AE60')
for pc in vp['bodies']:
    pc.set_facecolor('#5499C7')
    pc.set_alpha(1)
plt.sca(ax0)
plt.xticks([1], ['Distribution'])
plt.ylabel('Latency between '+str(_title)+' [ms]', fontsize=14)

scat = ax1.scatter(time, df.interval, alpha=.1, color='#5499C7')
scat.set_rasterized(True)
ax1.plot(time, filtered, color='#27AE60', lw=9)
ax1.yaxis.set_label_position("right")
ax1.yaxis.tick_right()
plt.sca(ax1)
plt.xlabel('Time [min]', fontsize=14)
plt.xlim([min(time), max(time)])
plt.grid(True)

ticks = []
avg = round(sum(df.interval)/len(df.interval),2)
index = 0
step = int(max(df.interval)/7)
prev = step
_set = False
for j in range(int(min(df.interval)), int(max(df.interval))+1, step):
    if (j >= avg and avg >= prev):
        ticks.append(avg)
        if (j-1>avg or j+1<avg): ticks.append(j)
        _set = True
    else:
        ticks.append(j)
    
    if (_set is False): index+=1
    prev = j
ticks.append(round(max(df.interval),2))
ticks[0] = round(min(df.interval), 2)

plt.yticks(ticks)
plt.gca().get_yticklabels()[-1].set_color('r')
plt.gca().get_yticklabels()[index].set_color('g')
plt.gca().get_yticklabels()[0].set_color('b')

fig.tight_layout()
plt.savefig(str(cluster)+'_'+str(_type)+'.svg', format="svg")
