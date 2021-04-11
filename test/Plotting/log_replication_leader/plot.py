import pandas as pd
import numpy as np
import matplotlib.pyplot as plt

log0 = pd.read_csv('log0.txt', encoding = "UTF-16 LE", header=None, skiprows=39, names=["ms","log","pc","node","step","b"])
log1 = pd.read_csv('log1.txt', encoding = "UTF-16 LE", header=None, skiprows=39, names=["ms","log","pc","node","step","b"])
log2 = pd.read_csv('log2.txt', encoding = "UTF-16 LE", header=None, skiprows=39, names=["ms","log","pc","node","step","b"])

fig = plt.figure(figsize=(10,5))
ax = fig.add_subplot(111)

# dataset
log1pc = np.array(log1.pc)
log0pc = np.array(log0.pc)
log2pc = np.array(log2.pc)
log1ms = np.array(log1.ms)#[i for i in range(len(log1pc))]#
log0ms = np.array(log0.ms)#[i for i in range(len(log0pc))]#
log2ms = np.array(log2.ms)#[i for i in range(len(log2pc))]#
log1node = np.array(log1.node)
log0node = np.array(log0.node)
log2node = np.array(log2.node)

log0ms=log0ms-log0ms[0]
log1ms=log1ms-log1ms[0]
log2ms=log2ms-log2ms[0]

_colors = dict()

_colors['log0'] = '#E67E22'
_colors['log1'] = '#52BE80'
_colors['log2'] = '#2980B9'

_zorders = dict()

offset1 = 100
offset2 = 50

if (log0pc[-1] > log1pc[-1] and log0pc[-1] > log2pc[-1]):
    _zorders['log0'] = 1
    log0pc = log0pc+offset1
    log0ms = log0ms+offset1
    if (log1pc[-1] > log2pc[-1]): 
        _zorders['log1'] = 2
        _zorders['log2'] = 3
        log1pc = log1pc+offset2
        log1ms = log1ms+offset2
    else:
        _zorders['log2'] = 2
        _zorders['log1'] = 3
        log2pc = log2pc+offset2
        log2ms = log2ms+offset2
elif (log1pc[-1] > log0pc[-1] and log1pc[-1] > log2pc[-1]):
    _zorders['log1'] = 1
    log1pc = log1pc+offset1
    log1ms = log1ms+offset1
    if (log0pc[-1] > log2pc[-1]):
        _zorders['log0'] = 2
        _zorders['log2'] = 3
        log0pc = log0pc+offset2
        log0ms = log0ms+offset2
    else:
        _zorders['log2'] = 2
        _zorders['log0'] = 3
        log2pc = log2pc+offset2
        log2ms = log2ms+offset2
elif (log2pc[-1] > log0pc[-1] and log2pc[-1] > log1pc[-1]):
    _zorders['log2'] = 1
    log2pc = log2pc+offset1
    log2ms = log2ms+offset1
    if (log1pc[-1] > log0pc[-1]):
        _zorders['log1'] = 2
        _zorders['log0'] = 3
        log1pc = log1pc+offset2
        log1ms = log1ms+offset2
    else:
        _zorders['log0'] = 2
        _zorders['log1'] = 3
        log0pc = log0pc+offset2
        log0ms = log0ms+offset2



print(log2node)


# polyline
ax.plot(log2ms, log2pc, zorder=_zorders['log2'], c=_colors['log2'], lw=4)
ax.plot(log0ms, log0pc, zorder=_zorders['log0'], c=_colors['log0'], lw=4)
ax.plot(log1ms, log1pc, zorder=_zorders['log1'], c=_colors['log1'], lw=4)
ax.scatter([log0ms[-1]], [log0pc[-1]], zorder=_zorders['log0'], marker="X", label="TcpNode0", c=_colors['log0'], s=150)
ax.scatter([log1ms[-1]], [log1pc[-1]], zorder=_zorders['log1'], marker="X", label="TcpNode1", c=_colors['log1'], s=150)
ax.scatter([log2ms[-1]], [log2pc[-1]], zorder=_zorders['log2'], marker="X", label="TcpNode2", c=_colors['log2'], s=150)
ax.set_ylabel("Request # [steps]", fontsize=12)
ax.set_xlabel("Time [milliseconds since start]", fontsize=12)

# custom
ax.annotate("Wait before start timer",
            xy=(((log1ms[2]-log1ms[1])/2)+log1ms[1], 100), xycoords='data',
            xytext=(log1ms[1]-1000, 500), textcoords='data',
            arrowprops=dict(arrowstyle="->",
                            connectionstyle="arc3"),
            )

# end markers
axT = ax.twinx()
if (_zorders['log2']==1):
    axT.plot(log2ms, log2node, zorder=_zorders['log2'], c='black', ls="--", label="# of nodes", lw=4)
    axT.plot(log0ms[:2], log0node[:2], zorder=_zorders['log0'], c='black', ls="--", lw=4)
    axT.plot(log1ms[:2], log1node[:2], zorder=_zorders['log1'], c='black', ls="--", lw=4)
elif (_zorders['log0']==1):
    axT.plot(log2ms[:2], log2node[:2], zorder=_zorders['log2'], c='black', ls="--", lw=4)
    axT.plot(log1ms[:2], log1node[:2], zorder=_zorders['log1'], c='black', ls="--", lw=4)
    axT.plot(log0ms, log0node, zorder=_zorders['log0'], c='black', ls="--", label="# of nodes", lw=4)
elif (_zorders['log1']==1):
    axT.plot(log2ms[:2], log2node[:2], zorder=_zorders['log2'], c='black', ls="--", lw=4)
    axT.plot(log0ms[:2], log0node[:2], zorder=_zorders['log0'], c='black', ls="--", lw=4)
    axT.plot(log1ms, log1node, zorder=_zorders['log1'], c='black', ls="--", label="# of nodes", lw=4)
axT.scatter([log0ms[-1]], [log0node[-1]], zorder=_zorders['log0'], marker="X", c=_colors['log0'], s=150)
axT.scatter([log1ms[-1]], [log1node[-1]], zorder=_zorders['log1'], marker="X", c=_colors['log1'], s=150)
axT.scatter([log2ms[-1]], [log2node[-1]], zorder=_zorders['log2'], marker="X", c=_colors['log2'], s=150)
axT.set_ylabel("# of nodes [cluster size]", fontsize=12)
axT.set_yticks([0,1,2])



# showing
ax.grid()
ax.legend(loc="center right", prop={'size': 16})
plt.tight_layout()
plt.show()