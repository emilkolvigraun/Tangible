import pandas as pd
amount = 20
cluster = 'timeout2'

# for i in range(1, amount+1):
# for i in [1, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50]:
for i in [1, 5, 10, 15, 20]:
    fn = str(cluster)+'/split/log_'+str(cluster)+'_'+str(i)+'.csv'
    print('Loading', '"'+fn+'"')
    df = pd.read_csv(fn)
    
    start = df.iloc[0].T0
    x = []
    y = []
    h = []
    val = []
    for j in range(i, len(df), i):
        T0 = df.iloc[j]['T0']
        T3 = df.iloc[j]['T3']
        T4 = df.iloc[j]['T4']
        t = (T0-start)
        if t==0:t=1
        x.append(t/1000)
        y.append((T3-T0)/1000)
        val.append(df.iloc[j]['val'])

    
    pd.DataFrame.from_dict({"x":x, "y":y, "val":val}).sort_values(by='val').to_csv(str(cluster)+'/treat/log_'+str(cluster)+'_'+str(i)+'.csv')