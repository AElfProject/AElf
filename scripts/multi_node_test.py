#!/usr/bin/env python3
# _*_ coding:utf-8 _*_
# Author:Liuyoushui
# Time = 2019/3/1 15:48

import os
import requests

_BASE_URL = "http://127.0.0.1:%s/%s/"
_HEADERS = {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
}

_BLOCK_INFO_PLAYLOAD = {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "GetBlockInfo",
    "params": {
        "blockHeight": "1",
        "includeTransactions": True
    }
}

_BLOCK_HEIGHT_PAYLOAD = {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "GetBlockHeight",
    "params": {
    }
}

_KEY_URLS = [
    'https://config-1257167156.cos.ap-chengdu.myqcloud.com/ELF_3DyQHandGuiirB5Co4Kagvr8cu6npfnFVeKbSpKhm1kqVht.ak',
    'https://config-1257167156.cos.ap-chengdu.myqcloud.com/ELF_3GM5SZPSnyTeDHQngudoFzU89JBevrfeRxBDDurkZdZNssi.ak',
    'https://config-1257167156.cos.ap-chengdu.myqcloud.com/ELF_Vhr4pHiaJTPeSFHM8NaUaJRsdaypcQ86CkgAgcHZkX41Zi.ak'
]

_CONF_URLS = {
    'node1': 'https://config-1257167156.cos.ap-chengdu.myqcloud.com/aelf1/appsettings.json',
    'node2': 'https://config-1257167156.cos.ap-chengdu.myqcloud.com/aelf2/appsettings.json',
    'node3': 'https://config-1257167156.cos.ap-chengdu.myqcloud.com/aelf3/appsettings.json'
}


def _start_node():
    """Start node"""
    print('1:Download the key........')
    os.system("mkdir -p ~/.local/share/aelf/keys")
    for url in _KEY_URLS:
        os.system('cd ~/.local/share/aelf/keys && wget %s' % url)

    print('2:Download configuration file............')
    for node in _CONF_URLS.keys():
        os.system('mkdir /tmp/%s && cd /tmp/%s && wget %s' % (node, node, _CONF_URLS[node]))

    print('3:Download the source code...........')
    os.system("cd /tmp && git clone  https://github.com/AElfProject/AElf.git /tmp/aelf-git")

    print('4:Compile the source code...........')
    os.system("cd /tmp/aelf-git && dotnet publish  -o /tmp/aelf-node")


    for node in _CONF_URLS.keys():
        print('Start node %s' % (node))
        os.system("cd /tmp/%s && nohup dotnet /tmp/aelf-node/AElf.Launcher.dll  > /tmp/myout1.file 2>&1 &" %(node))
        os.system("sleep 20")    

    # Waiting for gen blocks
    os.system("sleep 300")
    os.system("netstat -lntp")
    print('--------------------------------------------------------------')


def _request(url, payload):
    """Call chain RPC
    """
    response = requests.post(url, json=payload, headers=_HEADERS)
    return response.json()


def main():
    """Main process
    """
    try:
        _start_node()
    except Exception as e:
        print('error',e)
        exit(100)
    # Get block height
    try:
        node1 = _request(_BASE_URL % (8000, 'chain'), _BLOCK_HEIGHT_PAYLOAD)
        block_height = node1['result']

        print('Start check....\n >> block height: %s' % block_height)
        for i in range(2, block_height):
            _BLOCK_INFO_PLAYLOAD['params']['blockHeight'] = str(i)
            node1 = _request(_BASE_URL % (8000, 'chain'), _BLOCK_INFO_PLAYLOAD)
            node2 = _request(_BASE_URL % (8001, 'chain'), _BLOCK_INFO_PLAYLOAD)
            node3 = _request(_BASE_URL % (8002, 'chain'), _BLOCK_INFO_PLAYLOAD)
            node1_block_hash = node1['result'].get('BlockHash', '')
            node2_block_hash = node2['result'].get('BlockHash', '')
            node3_block_hash = node3['result'].get('BlockHash', '')
            if (node1_block_hash == node2_block_hash == node3_block_hash) and node1_block_hash != '':
                print(i, '--- ok --->', node1_block_hash, node2_block_hash, node3_block_hash)
                continue

            print("Missing match block hash at %s: %s, %s, %s" % (i, node1_block_hash, node2_block_hash, node3_block_hash))

            os.system("killall dotnet")
            exit(101)
    except Exception as e:
        print('error:',e)
        exit(102)
    print('Done.')


if __name__ == '__main__':
    main()
