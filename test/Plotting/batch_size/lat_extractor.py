import pandas as pd
amount = 20
cluster = 'timeout'

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
        T1 = df.iloc[j]['T0']
        T0 = df.iloc[j-i]['T0']
        de = (T1-T0)
        if de == 0: delay = 1
        else: delay = (T1-T0)/1000
        # if delay>200:continue
        y.append(delay)
        h.append(1000/delay)
        x.append((T1-start)/1000)
        val.append(df.iloc[j]['val'])
    
    pd.DataFrame.from_dict({"x":x, "y":y, "h":h, "val":val}).sort_values(by='val').to_csv(str(cluster)+'/lat/log_'+str(cluster)+'_'+str(i)+'.csv')