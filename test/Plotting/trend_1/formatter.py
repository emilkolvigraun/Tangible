import pandas as pd

cluster = 6
_type = 'send'
df = pd.read_csv(str(cluster)+'_'+str(_type)+'.csv')[-300000:]
df = df[((df.id >= 178499)&(df.id < 178512))]
# print(len(df))
# dct = {'time':[], 'id':[], 'interval':[]}
# for i in range(len(df)):
#     df_i = df.iloc[i]

#     val = df_i.interval
#     if (val>23): val+=2
#     if (val > 92): continue
#     dct['time'].append(df_i.time)
#     dct['id'].append(df_i.id)
#     dct['interval'].append(df_i.interval)

# df = pd.DataFrame.from_dict(dct)
# df.to_csv(str(cluster)+'_'+str(_type)+'.csv')

import matplotlib.pyplot as plt
sct = plt.scatter(df['id'], df['interval'])
sct.set_rasterized(True)
plt.grid(True)
plt.gca().ticklabel_format(useOffset=False)
plt.ylabel('Latency between consumption [ms]', fontsize=14)
plt.xlabel('request ID', fontsize=14)
plt.savefig('showcase.svg', format='svg')

