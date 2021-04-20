import pandas as pd
import matplotlib.pyplot as plt

cluster = 3
node = 2

df = pd.read_csv(str(cluster)+'/TcpNode'+str(node)+'.txt')

new = {'latency':[], 'id':[], 'time':[]}
T0 = df.iloc[0].send
for i in range(len(df)):
    df_i = df.iloc[i]

    # latency
    lat = (df_i.receive - df_i.send)/1000
    # if (lat>250):continue
    # time since start
    T = df_i.send - T0

    new['time'].append(T)

    # time between send and received
    new['latency'].append(lat)

    # id of request
    new['id'].append(df_i.id)

pd.DataFrame.from_dict(new).to_csv(str(cluster)+'_lat.csv')

plt.scatter(new['id'], new['latency'])
plt.show()